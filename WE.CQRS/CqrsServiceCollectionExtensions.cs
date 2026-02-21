using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using WE.CQRS.Behaviors;

namespace WE.CQRS;

public static class CqrsServiceCollectionExtensions
{
    public static IServiceCollection AddWECQRS(this IServiceCollection services,
        params Assembly[] assembliesToScan)
    {
        var assemblies = assembliesToScan.Length > 0
            ? assembliesToScan
            : new[] { Assembly.GetCallingAssembly(), typeof(CqrsServiceCollectionExtensions).Assembly };

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
        });

        // Add common behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // FluentValidation registration (scan for validators)
        services.AddValidatorsFromAssemblies(assemblies);

        return services;
    }
}