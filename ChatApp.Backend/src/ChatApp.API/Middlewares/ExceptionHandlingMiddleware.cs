using System.Text.Json;
using ChatApp.Application.Common.Exceptions;
using ChatApp.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var traceId = context.TraceIdentifier;

        ProblemDetails problemDetails;

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails = new ValidationProblemDetails(validationException.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Detail = "See the errors dictionary for validation error details.",
                    Instance = context.Request.Path
                };
                _logger.LogWarning(exception, "Validation failure on request {Path} (TraceId: {TraceId})", context.Request.Path, traceId);
                break;

            case NotFoundException notFoundException:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = notFoundException.Message,
                    Instance = context.Request.Path
                };
                _logger.LogWarning("Resource not found on request {Path}: {Message} (TraceId: {TraceId})", context.Request.Path, notFoundException.Message, traceId);
                break;

            case UnauthorizedException unauthorizedException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = unauthorizedException.Message,
                    Instance = context.Request.Path
                };
                _logger.LogWarning("Unauthorized access attempt on request {Path} (TraceId: {TraceId})", context.Request.Path, traceId);
                break;

            case ForbiddenAccessException forbiddenException:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = forbiddenException.Message,
                    Instance = context.Request.Path
                };
                _logger.LogWarning("Forbidden access on request {Path} (TraceId: {TraceId})", context.Request.Path, traceId);
                break;

            case DomainException domainException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Business rule violation",
                    Detail = domainException.Message,
                    Instance = context.Request.Path
                };
                _logger.LogWarning("Domain business rule violation on request {Path}: {Message} (TraceId: {TraceId})", context.Request.Path, domainException.Message, traceId);
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred",
                    Detail = "An unexpected internal server error occurred while processing your request.",
                    Instance = context.Request.Path
                };
                _logger.LogError(exception, "Unhandled exception occurred on request {Path} (TraceId: {TraceId})", context.Request.Path, traceId);
                break;
        }

        problemDetails.Extensions["traceId"] = traceId;

        var json = JsonSerializer.Serialize((object)problemDetails, problemDetails.GetType(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }
}
