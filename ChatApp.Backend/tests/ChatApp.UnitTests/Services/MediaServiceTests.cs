using System.Text;
using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Common.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ChatApp.UnitTests.Services;

public class MediaServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task UploadMedia_WhenValidImage_ShouldSaveFileAndPersistMediaAttachment()
    {
        // Arrange
        using var db = CreateDbContext();
        var storageMock = new Mock<IStorageService>();

        storageMock.Setup(s => s.SaveFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StorageFileResult(
                FileUrl: "/uploads/images/test.webp",
                ThumbnailUrl: "/uploads/images/test.webp",
                RelativePath: "uploads/images/test.webp",
                FileSizeBytes: 1024,
                ContentType: "image/webp"
            ));

        var sut = new MediaService(db, storageMock.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image-bytes"));

        // Act
        var result = await sut.UploadMediaAsync(stream, "sample.webp", "image/webp", MessageType.Image);

        // Assert
        result.Should().NotBeNull();
        result.FileUrl.Should().Be("/uploads/images/test.webp");
        result.ContentType.Should().Be("image/webp");
        result.MediaType.Should().Be(MessageType.Image);

        var persisted = await db.MediaAttachments.FirstOrDefaultAsync(m => m.Id == result.MediaId);
        persisted.Should().NotBeNull();
        persisted!.FileUrl.Should().Be("/uploads/images/test.webp");
        persisted.MessageId.Should().BeNull();
    }

    [Fact]
    public async Task UploadMedia_WhenEmptyStream_ShouldThrowValidationException()
    {
        // Arrange
        using var db = CreateDbContext();
        var storageMock = new Mock<IStorageService>();
        var sut = new MediaService(db, storageMock.Object);

        using var emptyStream = new MemoryStream();

        // Act & Assert
        var act = () => sut.UploadMediaAsync(emptyStream, "test.png", "image/png", MessageType.Image);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UploadMedia_WhenInvalidExtension_ShouldThrowValidationException()
    {
        // Arrange
        using var db = CreateDbContext();
        var storageMock = new Mock<IStorageService>();
        var sut = new MediaService(db, storageMock.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("malicious-bytes"));

        // Act & Assert
        var act = () => sut.UploadMediaAsync(stream, "test.exe", "application/x-msdownload", MessageType.Image);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetMediaById_WhenExists_ShouldReturnMediaDto()
    {
        // Arrange
        using var db = CreateDbContext();
        var storageMock = new Mock<IStorageService>();
        var sut = new MediaService(db, storageMock.Object);

        var mediaId = Guid.NewGuid();
        var attachment = new MediaAttachment
        {
            Id = mediaId,
            FileUrl = "/uploads/videos/sample.mp4",
            ContentType = "video/mp4",
            FileSizeBytes = 5000,
            UploadedAt = DateTime.UtcNow
        };

        db.MediaAttachments.Add(attachment);
        await db.SaveChangesAsync();

        // Act
        var result = await sut.GetMediaByIdAsync(mediaId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(mediaId);
        result.FileUrl.Should().Be("/uploads/videos/sample.mp4");
        result.ContentType.Should().Be("video/mp4");
    }

    [Fact]
    public async Task GetMediaById_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        using var db = CreateDbContext();
        var storageMock = new Mock<IStorageService>();
        var sut = new MediaService(db, storageMock.Object);

        // Act
        var result = await sut.GetMediaByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }
}
