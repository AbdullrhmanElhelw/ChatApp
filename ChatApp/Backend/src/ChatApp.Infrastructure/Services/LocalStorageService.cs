using Microsoft.AspNetCore.Hosting;
using ChatApp.Application.Common.Interfaces;

namespace ChatApp.Infrastructure.Services;

public class LocalStorageService : IStorageService
{
    private readonly IWebHostEnvironment _environment;

    public LocalStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<StorageFileResult> SaveFileAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        string subDirectory,
        CancellationToken cancellationToken = default)
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var sanitizedSubDir = subDirectory.Trim('/', '\\').Replace('\\', '/');
        var targetFolder = Path.Combine(webRoot, "uploads", sanitizedSubDir);

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        var extension = Path.GetExtension(originalFileName);
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        var safeFileName = $"{Guid.NewGuid():N}_{baseName}{extension}";
        var fullPath = Path.Combine(targetFolder, safeFileName);

        await using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        var fileInfo = new FileInfo(fullPath);
        var relativeUrl = $"/uploads/{sanitizedSubDir}/{safeFileName}";

        return new StorageFileResult(
            FileUrl: relativeUrl,
            ThumbnailUrl: contentType.StartsWith("image/") ? relativeUrl : null,
            RelativePath: Path.Combine("uploads", sanitizedSubDir, safeFileName),
            FileSizeBytes: fileInfo.Length,
            ContentType: contentType
        );
    }

    public Task<bool> DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, relativePath.TrimStart('/', '\\'));

        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(false);
    }
}
