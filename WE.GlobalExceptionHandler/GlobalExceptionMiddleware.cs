using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace WE.GlobalExceptionHandler;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger;
        _environment = environment;
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

        var problemDetails = CreateProblemDetails(context, exception);

        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        // Log with appropriate level
        if (context.Response.StatusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled server error. TraceId: {TraceId}", context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning("Client error: {StatusCode} - {Message}. TraceId: {TraceId}",
                context.Response.StatusCode, exception.Message, context.TraceIdentifier);
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(problemDetails, options);

        await context.Response.WriteAsync(json);
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            // Common built-in .NET / domain exceptions
            ArgumentException or ArgumentNullException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException or InvalidOperationException when exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status409Conflict,
            NotImplementedException => StatusCodes.Status501NotImplemented,
            OperationCanceledException => StatusCodes.Status408RequestTimeout,

            // You can add your own custom exceptions here
            // ValidationException (FluentValidation) => 400,
            // BusinessRuleViolationException => 409,
            // RateLimitExceededException => 429,

            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = _environment.IsDevelopment() ? exception.Message : "An error occurred while processing your request.",
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        // Add trace ID for correlation (very useful in logs)
        problem.Extensions["traceId"] = context.TraceIdentifier;

        // In development: show full exception details
        if (_environment.IsDevelopment())
        {
            problem.Extensions["exception"] = exception.ToString(); // or use ExceptionDetails helper
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        return problem;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        408 => "Request Timeout",
        409 => "Conflict",
        429 => "Too Many Requests",
        500 => "Internal Server Error",
        501 => "Not Implemented",
        _ => "Error"
    };
}