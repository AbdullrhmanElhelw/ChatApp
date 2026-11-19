using ChatApp.Application.Common.Exceptions;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChatApp.UnitTests.Services;

public class CoupleServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAndJoin_WhenValidInviteCode_ShouldPairBothPartners()
    {
        using var db = CreateDbContext();
        var user1 = new AppUser { Id = Guid.NewGuid(), UserName = "adam", FullName = "Adam", Email = "adam@test.com" };
        var user2 = new AppUser { Id = Guid.NewGuid(), UserName = "eve", FullName = "Eve", Email = "eve@test.com" };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var sut = new CoupleService(db);

        var created = await sut.CreateSpaceAsync(user1.Id);
        created.Data!.InviteCode.Should().HaveLength(6);
        created.Data.IsPaired.Should().BeFalse();

        var joined = await sut.JoinByCodeAsync(user2.Id, created.Data.InviteCode);
        joined.Data!.IsPaired.Should().BeTrue();
        joined.Data.Partner!.Id.Should().Be(user1.Id);

        var user1Space = await sut.GetMySpaceAsync(user1.Id);
        user1Space.Data!.IsPaired.Should().BeTrue();
        user1Space.Data.Partner!.Id.Should().Be(user2.Id);
    }

    [Fact]
    public async Task JoinByCode_WhenUsingOwnCode_ShouldThrowValidationException()
    {
        using var db = CreateDbContext();
        var user1 = new AppUser { Id = Guid.NewGuid(), UserName = "adam", FullName = "Adam", Email = "adam@test.com" };
        db.Users.Add(user1);
        await db.SaveChangesAsync();

        var sut = new CoupleService(db);
        var created = await sut.CreateSpaceAsync(user1.Id);

        var act = () => sut.JoinByCodeAsync(user1.Id, created.Data!.InviteCode);
        await act.Should().ThrowAsync<ValidationException>();
    }
}
