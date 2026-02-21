using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WE.RedisCache;

/// <summary>
/// Simplified Redis distributed cache registration with health checks.
/// </summary>
public static class RedisCacheServiceCollectionExtensions
{
    /// <summary>
    /// Adds StackExchange.Redis distributed cache using the provided connection string.
    /// Automatically adds Redis health check.
    /// </summary>
    public static IServiceCollection AddWERedisCache(
        this IServiceCollection services,
        string redisConnectionString,
        Action<RedisCacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "WE_"; // prefix to avoid collisions
            configure?.Invoke(options);
        });

        // Auto-add health check
        services.AddHealthChecks()
            .AddRedis(redisConnectionString, name: "redis-cache", tags: new[] { "cache", "redis" });

        return services;
    }
}