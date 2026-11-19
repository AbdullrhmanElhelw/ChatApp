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

    public async Task<ApiResponse<UserProfileDto>> UpdateCurrentUserProfileAsync(
        Guid userId,
        UpdateUserProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length < 2)
        {
            throw new ValidationException("Full name must be at least 2 characters.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        user.FullName = request.FullName.Trim()[..Math.Min(request.FullName.Trim().Length, 100)];
        user.Bio = string.IsNullOrWhiteSpace(request.Bio)
            ? null
            : request.Bio.Trim()[..Math.Min(request.Bio.Trim().Length, 300)];

        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
        {
            user.AvatarUrl = request.AvatarUrl.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

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

        return ApiResponse<UserProfileDto>.Ok(profileDto, "Profile updated successfully.");
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

        if (string.IsNullOrWhiteSpace(query))
        {
            return PagedResponse<UserProfileDto>.Create(new List<UserProfileDto>(), pageIndex, pageSize, 0);
        }

        var trimmed = query.Trim().TrimStart('@');
        if (trimmed.Length < 3)
        {
            return PagedResponse<UserProfileDto>.Create(new List<UserProfileDto>(), pageIndex, pageSize, 0);
        }

        var lower = trimmed.ToLowerInvariant();
        var baseQuery = _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id != currentUserId &&
                ((u.UserName != null && u.UserName.ToLower() == lower) ||
                 (u.Email != null && u.Email.ToLower() == lower)));

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
