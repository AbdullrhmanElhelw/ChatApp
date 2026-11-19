using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Users.DTOs;
using ChatApp.Application.Features.Users.Interfaces;
using ChatApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;

    public UserService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<UserProfileDto>> GetCurrentUserProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await GetUserByIdAsync(userId, cancellationToken);
    }

    public async Task<ApiResponse<UserProfileDto>> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        var profileDto = new UserProfileDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FullName,
            user.Bio,
            user.AvatarUrl,
            user.IsOnline,
            user.LastSeenAt,
            user.CreatedAt
        );

        return ApiResponse<UserProfileDto>.Ok(profileDto, "User profile retrieved successfully.");
    }

    public async Task<PagedResponse<UserProfileDto>> SearchUsersAsync(
        Guid currentUserId,
        string? query,
        int pageIndex = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var baseQuery = _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id != currentUserId);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var trimmed = query.Trim();
            baseQuery = baseQuery.Where(u =>
                (u.UserName != null && EF.Functions.Like(u.UserName, $"%{trimmed}%")) ||
                EF.Functions.Like(u.FullName, $"%{trimmed}%") ||
                (u.Email != null && EF.Functions.Like(u.Email, $"%{trimmed}%")));
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var users = await baseQuery
            .OrderBy(u => u.FullName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserProfileDto(
                u.Id,
                u.UserName ?? string.Empty,
                u.Email ?? string.Empty,
                u.FullName,
                u.Bio,
                u.AvatarUrl,
                u.IsOnline,
                u.LastSeenAt,
                u.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return PagedResponse<UserProfileDto>.Create(users, pageIndex, pageSize, totalCount);
    }

    public async Task UpdatePresenceAsync(Guid userId, bool isOnline, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null)
        {
            user.IsOnline = isOnline;
            user.LastSeenAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
