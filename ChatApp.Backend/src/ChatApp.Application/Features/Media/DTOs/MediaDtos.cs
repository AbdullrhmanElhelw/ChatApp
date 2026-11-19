using ChatApp.Domain.Enums;

namespace ChatApp.Application.Features.Media.DTOs;

public record UploadMediaResponseDto(
    Guid MediaId,
    string FileUrl,
    string? ThumbnailUrl,
    string ContentType,
    long FileSizeBytes,
    int? Width,
    int? Height,
    int? DurationSeconds,
    MessageType MediaType,
    DateTime UploadedAt
);

public record MediaAttachmentDto(
    Guid Id,
    Guid? MessageId,
    string FileUrl,
    string? ThumbnailUrl,
    string ContentType,
    long FileSizeBytes,
    int? Width,
    int? Height,
    int? DurationSeconds,
    DateTime UploadedAt
);
