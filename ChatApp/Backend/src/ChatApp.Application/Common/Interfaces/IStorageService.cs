namespace ChatApp.Application.Common.Interfaces;

public record StorageFileResult(
    string FileUrl,
    string? ThumbnailUrl,
    string RelativePath,
    long FileSizeBytes,
    string ContentType
);

public interface IStorageService
{
    Task<StorageFileResult> SaveFileAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        string subDirectory,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
}
