using ChatApp.Application.Common.Exceptions;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChatApp.UnitTests.Services;

public class UserServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SearchUsers_WhenQueryIsNull_ShouldReturnEmptyForPrivacy()
    {
        // Arrange
        using var db = CreateDbContext();
        var currentUser = new AppUser { Id = Guid.NewGuid(), UserName = "current_user", FullName = "Current User", Email = "current@test.com" };
        var otherUser = new AppUser { Id = Guid.NewGuid(), UserName = "other_user", FullName = "Other User", Email = "other@test.com" };
        db.Users.AddRange(currentUser, otherUser);
        await db.SaveChangesAsync();

        var sut = new UserService(db);

        // Act
        var result = await sut.SearchUsersAsync(currentUser.Id, query: null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchUsers_WithExactUsername_ShouldReturnMatchingUser()
    {
        // Arrange
        using var db = CreateDbContext();
        var currentUser = new AppUser { Id = Guid.NewGuid(), UserName = "me", FullName = "Me", Email = "me@test.com" };
        var user1 = new AppUser { Id = Guid.NewGuid(), UserName = "alex_brown", FullName = "Alex Brown", Email = "alex@test.com" };
        var user2 = new AppUser { Id = Guid.NewGuid(), UserName = "sam_smith", FullName = "Sam Smith", Email = "sam@test.com" };
        db.Users.AddRange(currentUser, user1, user2);
        await db.SaveChangesAsync();

        var sut = new UserService(db);

        // Act
        var result = await sut.SearchUsersAsync(currentUser.Id, query: "@alex_brown");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data.First().Id.Should().Be(user1.Id);
    }

    [Fact]
    public async Task GetUserById_WhenNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new UserService(db);

        // Act
        var act = () => sut.GetUserByIdAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateCurrentUserProfile_WhenValid_ShouldPersistFullNameBioAndAvatar()
    {
        // Arrange
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser
        {
            Id = userId,
            UserName = "khaled",
            FullName = "Old Name",
            Email = "khaled@test.com"
        });
        await db.SaveChangesAsync();

        var sut = new UserService(db);
        var req = new ChatApp.Application.Features.Users.DTOs.UpdateUserProfileRequestDto(
            "Khaled Hassan",
            "BetweenUs 💜",
            "/uploads/images/avatar.png");

        // Act
        var result = await sut.UpdateCurrentUserProfileAsync(userId, req);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.FullName.Should().Be("Khaled Hassan");
        result.Data.Bio.Should().Be("BetweenUs 💜");
        result.Data.AvatarUrl.Should().Be("/uploads/images/avatar.png");

        var saved = await db.Users.FindAsync(userId);
        saved!.FullName.Should().Be("Khaled Hassan");
        saved.Bio.Should().Be("BetweenUs 💜");
        saved.AvatarUrl.Should().Be("/uploads/images/avatar.png");
    }

    [Fact]
    public async Task UpdateCurrentUserProfile_WhenFullNameTooShort_ShouldThrowValidationException()
    {
        // Arrange
        using var db = CreateDbContext();
        var sut = new UserService(db);
        var req = new ChatApp.Application.Features.Users.DTOs.UpdateUserProfileRequestDto("A", null, null);

        // Act
        var act = () => sut.UpdateCurrentUserProfileAsync(Guid.NewGuid(), req);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
