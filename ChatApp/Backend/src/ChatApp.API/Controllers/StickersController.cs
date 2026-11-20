using System.Security.Claims;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Stickers.DTOs;
using ChatApp.Application.Features.Stickers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/stickers")]
public class StickersController : ControllerBase
{
    private readonly IStickerService _stickerService;

    public StickersController(IStickerService stickerService)
    {
        _stickerService = stickerService;
    }

    [HttpGet("packs")]
    [ProducesResponseType(typeof(ApiResponse<List<StickerPackDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicPacks(CancellationToken cancellationToken)
    {
        var packs = await _stickerService.GetPublicPacksAsync(cancellationToken);
        return Ok(ApiResponse<List<StickerPackDto>>.Ok(packs));
    }

    [HttpGet("my-packs")]
    [ProducesResponseType(typeof(ApiResponse<List<StickerPackDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPacks(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var packs = await _stickerService.GetUserInstalledPacksAsync(currentUserId, cancellationToken);
        return Ok(ApiResponse<List<StickerPackDto>>.Ok(packs));
    }

    [HttpGet("packs/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StickerPackDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPackById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var pack = await _stickerService.GetPackByIdAsync(id, cancellationToken);
        if (pack == null)
        {
            return NotFound(new ProblemDetails { Title = "حزمة الاستيكرات غير موجودة", Status = StatusCodes.Status404NotFound });
        }

        return Ok(ApiResponse<StickerPackDto>.Ok(pack));
    }

    [HttpPost("packs")]
    [ProducesResponseType(typeof(ApiResponse<StickerPackDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePack(
        [FromBody] CreateStickerPackRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var pack = await _stickerService.CreatePackAsync(currentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<StickerPackDto>.Ok(pack, "تم إنشاء حزمة الاستيكرات بنجاح"));
    }

    [HttpPost("packs/{id:guid}/stickers")]
    [ProducesResponseType(typeof(ApiResponse<StickerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddSticker(
        [FromRoute] Guid id,
        [FromBody] AddStickerRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var sticker = await _stickerService.AddStickerToPackAsync(currentUserId, id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<StickerDto>.Ok(sticker, "تمت إضافة الاستيكر بنجاح"));
    }

    [HttpPost("packs/{id:guid}/install")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> InstallPack([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var success = await _stickerService.InstallPackAsync(currentUserId, id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(success, "تم تثبيت الحزمة"));
    }

    [HttpDelete("packs/{id:guid}/uninstall")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UninstallPack([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var success = await _stickerService.UninstallPackAsync(currentUserId, id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(success, "تمت إزالة تثبيت الحزمة"));
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
        {
            throw new UnauthorizedAccessException("المستخدم غير مصرح به.");
        }

        return userId;
    }
}
