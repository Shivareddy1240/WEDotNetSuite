using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers;

namespace WE.SerilogSetup;

public static class SerilogHostExtensions
{
    public static IHostBuilder UseWESerilog(this IHostBuilder builder)
    {
        builder.UseSerilog((ctx, services, cfg) =>
        {
            cfg
                .ReadFrom.Configuration(ctx.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId()
                .Enrich.WithEnvironmentName()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}");

            // Optional: request logging middleware (very common)
            // app.UseSerilogRequestLogging();  // call this in Program.cs pipeline
        });

        return builder;
    }
}