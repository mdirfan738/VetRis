using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VetRIS.Domain.Exceptions;
using AppValidationException = VetRIS.Application.Common.Exceptions.ValidationException;

namespace VetRIS.Api.Middleware;

/// <summary>
/// .NET 8's IExceptionHandler pipeline (registered via AddExceptionHandler +
/// app.UseExceptionHandler() with no args) — every unhandled exception from
/// a MediatR handler lands here exactly once, mapped to a status code the
/// React app can branch on instead of a raw 500 + stack trace.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            AppValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            InvalidStudyStatusTransitionException => (StatusCodes.Status409Conflict, "Invalid study status transition"),
            InvalidReportStatusTransitionException => (StatusCodes.Status409Conflict, "Invalid report status transition"),
            DomainException => (StatusCodes.Status400BadRequest, "Business rule violation"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception is AppValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
