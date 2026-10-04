using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WeSpace.Api.Application.Common.Exceptions;

namespace WeSpace.Api.API.Middleware;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationException => HandleValidationException(httpContext, validationException),
            NotFoundException notFoundException => HandleNotFoundException(httpContext, notFoundException),
            ConflictException conflictException => HandleConflictException(httpContext, conflictException),
            UnauthorizedException unauthorizedException => HandleUnauthorizedException(httpContext, unauthorizedException),
            UnauthorizedAccessException unauthorizedException => HandleUnauthorizedException(httpContext, unauthorizedException),
            _ => HandleUnhandledException(httpContext, exception)
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int, ProblemDetails) HandleValidationException(HttpContext context, ValidationException ex)
    {
        var problemDetails = new HttpValidationProblemDetails(ex.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        return (StatusCodes.Status400BadRequest, problemDetails);
    }

    private static (int, ProblemDetails) HandleNotFoundException(HttpContext context, NotFoundException ex)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource Not Found",
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        return (StatusCodes.Status404NotFound, problemDetails);
    }

    private static (int, ProblemDetails) HandleConflictException(HttpContext context, ConflictException ex)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        return (StatusCodes.Status409Conflict, problemDetails);
    }

    private static (int, ProblemDetails) HandleUnauthorizedException(HttpContext context, Exception ex)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        return (StatusCodes.Status401Unauthorized, problemDetails);
    }

    private (int, ProblemDetails) HandleUnhandledException(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);

        var isDevelopment = _environment.IsDevelopment();

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
            Detail = isDevelopment
                ? ex.Message
                : "An unexpected error occurred. Please try again later.",
            Instance = context.Request.Path
        };

        if (isDevelopment)
        {
            problemDetails.Extensions["stackTrace"] = ex.StackTrace;
        }

        return (StatusCodes.Status500InternalServerError, problemDetails);
    }
}
