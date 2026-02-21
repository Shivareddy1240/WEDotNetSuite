using Microsoft.AspNetCore.Builder;

namespace WE.GlobalExceptionHandler;

/// <summary>
/// Extension methods for adding global exception handling middleware.
/// </summary>
public static class GlobalExceptionExtensions
{
    /// <summary>
    /// Adds global exception handling middleware to the application pipeline.
    /// Catches unhandled exceptions and returns standardized ProblemDetails JSON.
    /// Place this early in the pipeline (before other middleware).
    /// </summary>
    public static IApplicationBuilder UseWEGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}