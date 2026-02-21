using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;  // ← for AddHealthChecks


namespace WE.EfCoreHelpers;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers DbContext (transient or pooled) with SQL Server, auditing, soft-delete, 
    /// optional multi-tenancy, and health checks.
    /// </summary>
    public static IServiceCollection AddWEDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        bool enableMultiTenancy = false,
        bool enablePooling = false,
        int poolSize = 128,
        Action<DbContextOptionsBuilder>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Temporary provider to resolve dependencies during registration
        using var tempProvider = services.BuildServiceProvider();
        var currentUser = tempProvider.GetRequiredService<ICurrentUserService>();

        Action<DbContextOptionsBuilder> optionsAction = options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
            });

            options.AddInterceptors(new AuditAndSoftDeleteInterceptor(currentUser));

            configure?.Invoke(options);
        };

        if (enablePooling)
        {
            services.AddDbContextPool<TContext>(optionsAction, poolSize);
        }
        else
        {
            services.AddDbContext<TContext>(optionsAction);
        }

        // IHttpContextAccessor (manual registration - safe and lightweight)
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Current user service
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Tenant accessor if needed
        if (enableMultiTenancy)
        {
            services.AddScoped<ITenantAccessor, TenantAccessor>();
        }

        // Health check for the DbContext
        services.AddHealthChecks()
    .AddDbContextCheck<TContext>(
    name: "efcore-db",
    tags: new[] { "db", "efcore" },
    customTestQuery: (context, token) => context.Database.CanConnectAsync(token)
);

        return services;
    }
}