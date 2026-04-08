using System.Net.Mime;
using EduZim.Application.Exceptions;
using EduZim.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EduZim.API.ExceptionHandling;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, problem) = MapException(exception);
        if (statusCode >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");
        else
            logger.LogWarning(exception, "Request failed: {Message}", exception.Message);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int StatusCode, ProblemDetails Problem) MapException(Exception exception)
    {
        return exception switch
        {
            TenantAccessViolationException ex => (
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = ex.Message,
                    Type = "https://eduzim.co.zw/errors/tenant-access",
                }),
            DomainException ex => (
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = ex.Message,
                    Type = "https://eduzim.co.zw/errors/domain",
                }),
            NotFoundException ex => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Title = "Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = ex.Message,
                    Type = "https://eduzim.co.zw/errors/not-found",
                }),
            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                new ValidationProblemDetails(
                    ex.Errors.ToDictionary(
                        e => e.Key,
                        e => e.Value))
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://eduzim.co.zw/errors/validation",
                }),
            ConflictException ex => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Title = "Conflict",
                    Status = StatusCodes.Status409Conflict,
                    Detail = ex.Message,
                    Type = "https://eduzim.co.zw/errors/conflict",
                }),
            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Server Error",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "An unexpected error occurred.",
                    Type = "https://eduzim.co.zw/errors/internal",
                }),
        };
    }
}
