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
    public async Task SearchUsers_ShouldExcludeCurrentUser()
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
        result.Data.Should().HaveCount(1);
        result.Data.First().Id.Should().Be(otherUser.Id);
    }

    [Fact]
    public async Task SearchUsers_WithMatchingQuery_ShouldFilterByNameOrUsername()
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
        var result = await sut.SearchUsersAsync(currentUser.Id, query: "alex");

        // Assert
        result.Data.Should().HaveCount(1);
        result.Data.First().UserName.Should().Be("alex_brown");
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
}
