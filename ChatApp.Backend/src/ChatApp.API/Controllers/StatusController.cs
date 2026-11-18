using ChatApp.Application.Common.Interfaces;
using ChatApp.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.API.Controllers;

public class StatusController : ApiControllerBase
{
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;

    public StatusController(IDateTimeService dateTimeService, ICurrentUserService currentUserService)
    {
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ServerStatusDto>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<ServerStatusDto>> GetStatus()
    {
        var status = new ServerStatusDto
        {
            Status = "Healthy",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Framework = ".NET 10.0",
            ServerTimeUtc = _dateTimeService.UtcNow,
            IsAuthenticated = _currentUserService.IsAuthenticated,
            UserId = _currentUserService.UserId
        };

        return Ok(ApiResponse<ServerStatusDto>.Ok(status, "ChatApp API is running normally."));
    }
}

public class ServerStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public DateTime ServerTimeUtc { get; set; }
    public bool IsAuthenticated { get; set; }
    public Guid? UserId { get; set; }
}
