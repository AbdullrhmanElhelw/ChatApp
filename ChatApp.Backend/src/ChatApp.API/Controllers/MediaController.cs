using ChatApp.Application.Common.Exceptions;
using ChatApp.Application.Common.Models;
using ChatApp.Application.Features.Media.DTOs;
using ChatApp.Application.Features.Media.Interfaces;
using ChatApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/media")]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<UploadMediaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadMedia(
        IFormFile file,
        [FromForm] int mediaType = (int)MessageType.Image,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationException("لم يتم تحديد أي ملف للرفع.");
        }

        var parsedMediaType = Enum.IsDefined(typeof(MessageType), mediaType)
            ? (MessageType)mediaType
            : MessageType.Image;

        await using var stream = file.OpenReadStream();
        var result = await _mediaService.UploadMediaAsync(
            stream,
            file.FileName,
            file.ContentType,
            parsedMediaType,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ApiResponse<UploadMediaResponseDto>.Ok(result, "تم رفع الملف بنجاح"));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MediaAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMediaById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediaService.GetMediaByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound(new ProblemDetails { Title = "الملف غير موجود", Status = StatusCodes.Status404NotFound });
        }

        return Ok(ApiResponse<MediaAttachmentDto>.Ok(result));
    }
}
