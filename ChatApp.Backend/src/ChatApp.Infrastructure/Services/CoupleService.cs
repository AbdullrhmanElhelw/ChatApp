using System.Security.Cryptography;
using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Couples.DTOs;
using ChatApp.Application.Features.Couples.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Services;

public class CoupleService : ICoupleService
{
    private readonly AppDbContext _dbContext;
    private const string InviteAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public CoupleService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<CoupleSpaceDto?>> GetMySpaceAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var space = await FindSpaceForUserAsync(currentUserId, cancellationToken);
        if (space == null)
        {
            return ApiResponse<CoupleSpaceDto?>.Ok(null, "No couple space found.");
        }

        return ApiResponse<CoupleSpaceDto?>.Ok(MapToDto(space, currentUserId));
    }

    public async Task<ApiResponse<CoupleSpaceDto>> CreateSpaceAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var existing = await FindSpaceForUserAsync(currentUserId, cancellationToken);
        if (existing != null)
        {
            return ApiResponse<CoupleSpaceDto>.Ok(MapToDto(existing, currentUserId), "Existing couple space returned.");
        }

        var code = await GenerateUniqueInviteCodeAsync(cancellationToken);
        var space = new CoupleSpace
        {
            Id = Guid.NewGuid(),
            Partner1Id = currentUserId,
            InviteCode = code,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CoupleSpaces.Add(space);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var loaded = await FindSpaceForUserAsync(currentUserId, cancellationToken) ?? space;
        return ApiResponse<CoupleSpaceDto>.Ok(MapToDto(loaded, currentUserId), "Couple space created.");
    }

    public async Task<ApiResponse<CoupleSpaceDto>> JoinByCodeAsync(Guid currentUserId, string inviteCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
        {
            throw new ValidationException("كود الدعوة مطلوب.");
        }

        var normalized = inviteCode.Trim().ToUpperInvariant();

        var existingForUser = await FindSpaceForUserAsync(currentUserId, cancellationToken);
        if (existingForUser != null && existingForUser.Partner2Id.HasValue)
        {
            throw new ValidationException("حسابك مربوط بالفعل بشريك آخر.");
        }

        var targetSpace = await _dbContext.CoupleSpaces
            .Include(cs => cs.Partner1)
            .Include(cs => cs.Partner2)
            .FirstOrDefaultAsync(cs => cs.InviteCode == normalized, cancellationToken);

        if (targetSpace == null)
        {
            throw new NotFoundException("كود الدعوة غير صحيح أو غير موجود.");
        }

        if (targetSpace.Partner1Id == currentUserId)
        {
            throw new ValidationException("لا يمكنك استخدام كود الدعوة الخاص بك.");
        }

        if (targetSpace.Partner2Id.HasValue && targetSpace.Partner2Id != currentUserId)
        {
            throw new ValidationException("هذه المساحة مكتملة بالفعل.");
        }

        // Remove any unpaired solo space created by Partner 2 before joining
        if (existingForUser != null && !existingForUser.Partner2Id.HasValue && existingForUser.Id != targetSpace.Id)
        {
            _dbContext.CoupleSpaces.Remove(existingForUser);
        }

        targetSpace.Partner2Id = currentUserId;
        targetSpace.PairedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var reloaded = await FindSpaceForUserAsync(currentUserId, cancellationToken) ?? targetSpace;
        return ApiResponse<CoupleSpaceDto>.Ok(MapToDto(reloaded, currentUserId), "تم ربط الحسابين بنجاح!");
    }

    public async Task<ApiResponse<CoupleSpaceDto>> UpdateMoodAsync(Guid currentUserId, string mood, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mood))
        {
            throw new ValidationException("الحالة المزاجية مطلوبة.");
        }

        var space = await FindSpaceForUserAsync(currentUserId, cancellationToken);
        if (space == null)
        {
            // Auto-create a solo space so mood can always be stored
            await CreateSpaceAsync(currentUserId, cancellationToken);
            space = await FindSpaceForUserAsync(currentUserId, cancellationToken)
                ?? throw new NotFoundException("Couple space not found.");
        }

        var trimmed = mood.Trim();
        if (trimmed.Length > 64) trimmed = trimmed[..64];

        if (space.Partner1Id == currentUserId)
        {
            space.Partner1Mood = trimmed;
        }
        else
        {
            space.Partner2Mood = trimmed;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ApiResponse<CoupleSpaceDto>.Ok(MapToDto(space, currentUserId), "تم تحديث المزاج.");
    }

    public async Task<ApiResponse<CoupleSpaceDto>> SyncSpaceAsync(Guid currentUserId, SyncCoupleSpaceRequestDto request, CancellationToken cancellationToken = default)
    {
        var space = await FindSpaceForUserAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("لم يتم العثور على مساحة مشتركة.");

        if (request.SpecialDatesJson != null) space.SpecialDatesJson = request.SpecialDatesJson;
        if (request.MemoriesJson != null) space.MemoriesJson = request.MemoriesJson;
        if (request.LoveNotesJson != null) space.LoveNotesJson = request.LoveNotesJson;
        if (request.DailyAnswersJson != null) space.DailyAnswersJson = request.DailyAnswersJson;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ApiResponse<CoupleSpaceDto>.Ok(MapToDto(space, currentUserId), "تمت المزامنة بنجاح.");
    }

    public async Task<ApiResponse<bool>> UnlinkAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var space = await FindSpaceForUserAsync(currentUserId, cancellationToken);
        if (space == null)
        {
            return ApiResponse<bool>.Ok(true);
        }

        _dbContext.CoupleSpaces.Remove(space);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Ok(true, "تم فك الربط بنجاح.");
    }

    private Task<CoupleSpace?> FindSpaceForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.CoupleSpaces
            .Include(cs => cs.Partner1)
            .Include(cs => cs.Partner2)
            .FirstOrDefaultAsync(cs => cs.Partner1Id == userId || cs.Partner2Id == userId, cancellationToken);
    }

    private async Task<string> GenerateUniqueInviteCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var chars = new char[6];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = InviteAlphabet[RandomNumberGenerator.GetInt32(InviteAlphabet.Length)];
            }
            var code = new string(chars);
            var exists = await _dbContext.CoupleSpaces.AnyAsync(cs => cs.InviteCode == code, cancellationToken);
            if (!exists) return code;
        }

        return Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }

    private static CoupleSpaceDto MapToDto(CoupleSpace space, Guid currentUserId)
    {
        var isPartner1 = space.Partner1Id == currentUserId;
        var myMood = isPartner1 ? space.Partner1Mood : space.Partner2Mood;
        var partnerUser = isPartner1 ? space.Partner2 : space.Partner1;
        var partnerMood = isPartner1 ? space.Partner2Mood : space.Partner1Mood;

        CouplePartnerDto? partnerDto = partnerUser == null
            ? null
            : new CouplePartnerDto(
                partnerUser.Id,
                partnerUser.UserName ?? string.Empty,
                partnerUser.FullName,
                partnerUser.AvatarUrl,
                partnerUser.IsOnline,
                partnerUser.LastSeenAt,
                partnerMood);

        return new CoupleSpaceDto(
            space.Id,
            space.InviteCode,
            space.Partner2Id.HasValue,
            space.PairedAt,
            myMood,
            partnerDto,
            space.SpecialDatesJson,
            space.MemoriesJson,
            space.LoveNotesJson,
            space.DailyAnswersJson);
    }
}
