using System.Security.Claims;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Couples.DTOs;
using ChatApp.Application.Features.Couples.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/couples")]
public class CouplesController : ControllerBase
{
    private readonly ICoupleService _coupleService;

    public CouplesController(ICoupleService coupleService)
    {
        _coupleService = coupleService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CoupleSpaceDto?>>> GetMySpace(CancellationToken cancellationToken)
    {
        return Ok(await _coupleService.GetMySpaceAsync(GetCurrentUserId(), cancellationToken));
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<CoupleSpaceDto>>> CreateSpace(CancellationToken cancellationToken)
    {
        return Ok(await _coupleService.CreateSpaceAsync(GetCurrentUserId(), cancellationToken));
    }

    [HttpPost("join")]
    public async Task<ActionResult<ApiResponse<CoupleSpaceDto>>> JoinByCode(
        [FromBody] JoinCoupleRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _coupleService.JoinByCodeAsync(GetCurrentUserId(), request.InviteCode, cancellationToken));
    }

    [HttpPut("mood")]
    public async Task<ActionResult<ApiResponse<CoupleSpaceDto>>> UpdateMood(
        [FromBody] UpdateMoodRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _coupleService.UpdateMoodAsync(GetCurrentUserId(), request.Mood, cancellationToken));
    }

    [HttpPut("sync")]
    public async Task<ActionResult<ApiResponse<CoupleSpaceDto>>> SyncSpace(
        [FromBody] SyncCoupleSpaceRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _coupleService.SyncSpaceAsync(GetCurrentUserId(), request, cancellationToken));
    }

    [HttpPost("unlink")]
    public async Task<ActionResult<ApiResponse<bool>>> Unlink(CancellationToken cancellationToken)
    {
        return Ok(await _coupleService.UnlinkAsync(GetCurrentUserId(), cancellationToken));
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                  User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
    }
}
