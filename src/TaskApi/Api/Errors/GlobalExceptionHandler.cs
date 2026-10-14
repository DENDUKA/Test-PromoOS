using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskApi.Domain.Common;

namespace TaskApi.Api.Errors;

/// <summary>Последняя линия обороны: наружу уходит безопасный ProblemDetails, детали — только в лог.</summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, expose) = exception switch
        {
            DomainException => (StatusCodes.Status409Conflict, "Conflict", true),
            BadHttpRequestException bad => (bad.StatusCode, "Bad request", true),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", false),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request rejected for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = expose ? exception.Message : null,
            },
        });
    }
}
