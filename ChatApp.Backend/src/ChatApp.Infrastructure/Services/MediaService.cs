using Microsoft.EntityFrameworkCore;
using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Common.Interfaces;
using ChatApp.Application.Features.Media.DTOs;
using ChatApp.Application.Features.Media.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Infrastructure.Persistence;

namespace ChatApp.Infrastructure.Services;

public class MediaService : IMediaService
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private static readonly HashSet<string> AllowedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov", ".mkv"
    };

    private static readonly HashSet<string> AllowedAudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".m4a", ".wav", ".ogg", ".aac"
    };

    private static readonly HashSet<string> AllowedStickerExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".webp", ".png"
    };

    public MediaService(AppDbContext context, IStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    public async Task<UploadMediaResponseDto> UploadMediaAsync(
        Stream stream,
        string fileName,
        string contentType,
        MessageType mediaType,
        CancellationToken cancellationToken = default)
    {
        if (stream == null || stream.Length == 0)
        {
            throw new ValidationException("الملف المرفوع فارغ.");
        }

        var extension = Path.GetExtension(fileName);
        ValidateFile(extension, contentType, mediaType, stream.Length);

        var subDirectory = mediaType switch
        {
            MessageType.Image => "images",
            MessageType.Video => "videos",
            MessageType.Audio => "audio",
            MessageType.Sticker => "stickers",
            _ => "misc"
        };

        var storageResult = await _storageService.SaveFileAsync(
            stream,
            fileName,
            contentType,
            subDirectory,
            cancellationToken);

        var mediaAttachment = new MediaAttachment
        {
            Id = Guid.NewGuid(),
            MessageId = null,
            FileUrl = storageResult.FileUrl,
            ThumbnailUrl = storageResult.ThumbnailUrl,
            ContentType = storageResult.ContentType,
            FileSizeBytes = storageResult.FileSizeBytes,
            Width = mediaType == MessageType.Sticker ? 512 : null,
            Height = mediaType == MessageType.Sticker ? 512 : null,
            DurationSeconds = null,
            UploadedAt = DateTime.UtcNow
        };

        _context.MediaAttachments.Add(mediaAttachment);
        await _context.SaveChangesAsync(cancellationToken);

        return new UploadMediaResponseDto(
            MediaId: mediaAttachment.Id,
            FileUrl: mediaAttachment.FileUrl,
            ThumbnailUrl: mediaAttachment.ThumbnailUrl,
            ContentType: mediaAttachment.ContentType,
            FileSizeBytes: mediaAttachment.FileSizeBytes,
            Width: mediaAttachment.Width,
            Height: mediaAttachment.Height,
            DurationSeconds: mediaAttachment.DurationSeconds,
            MediaType: mediaType,
            UploadedAt: mediaAttachment.UploadedAt
        );
    }

    public async Task<MediaAttachmentDto?> GetMediaByIdAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var media = await _context.MediaAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == mediaId, cancellationToken);

        if (media == null) return null;

        return new MediaAttachmentDto(
            Id: media.Id,
            MessageId: media.MessageId,
            FileUrl: media.FileUrl,
            ThumbnailUrl: media.ThumbnailUrl,
            ContentType: media.ContentType,
            FileSizeBytes: media.FileSizeBytes,
            Width: media.Width,
            Height: media.Height,
            DurationSeconds: media.DurationSeconds,
            UploadedAt: media.UploadedAt
        );
    }

    private static void ValidateFile(string extension, string contentType, MessageType mediaType, long sizeBytes)
    {
        const long maxImageSize = 20 * 1024 * 1024; // 20 MB
        const long maxVideoSize = 50 * 1024 * 1024; // 50 MB
        const long maxAudioSize = 30 * 1024 * 1024; // 30 MB
        const long maxStickerSize = 5 * 1024 * 1024; // 5 MB

        switch (mediaType)
        {
            case MessageType.Image:
                if (!AllowedImageExtensions.Contains(extension))
                    throw new ValidationException($"صيغة الصورة غير مدعومة ({extension}). الصيغ المتاحة: jpg, jpeg, png, webp, gif.");
                if (sizeBytes > maxImageSize)
                    throw new ValidationException("حجم الصورة يتجاوز الحد الأقصى المسموح (20 ميجابايت).");
                break;

            case MessageType.Video:
                if (!AllowedVideoExtensions.Contains(extension))
                    throw new ValidationException($"صيغة الفيديو غير مدعومة ({extension}). الصيغ المتاحة: mp4, webm, mov, mkv.");
                if (sizeBytes > maxVideoSize)
                    throw new ValidationException("حجم الفيديو يتجاوز الحد الأقصى المسموح (50 ميجابايت).");
                break;

            case MessageType.Audio:
                if (!AllowedAudioExtensions.Contains(extension))
                    throw new ValidationException($"صيغة الصوت غير مدعومة ({extension}). الصيغ المتاحة: mp3, m4a, wav, ogg, aac.");
                if (sizeBytes > maxAudioSize)
                    throw new ValidationException("حجم الملف الصوتي يتجاوز الحد الأقصى المسموح (30 ميجابايت).");
                break;

            case MessageType.Sticker:
                if (!AllowedStickerExtensions.Contains(extension))
                    throw new ValidationException($"صيغة الملصق غير مدعومة ({extension}). الصيغ المتاحة: webp, png.");
                if (sizeBytes > maxStickerSize)
                    throw new ValidationException("حجم الملصق يتجاوز الحد الأقصى المسموح (5 ميجابايت).");
                break;

            default:
                throw new ValidationException("نوع الوسائط غير معروف.");
        }
    }
}
