WE .NET Suite: User Guide and Documentation
Welcome to the WE .NET Suite — a collection of lightweight, opinionated NuGet packages designed to simplify common .NET development tasks. Our philosophy is "one line away": install a package, add one or two lines of code, and you're done with sensible defaults. These packages are built for .NET 8+, targeting ASP.NET Core web apps, but many are usable in other project types.
This document covers all implemented features across the packages, with explanations, installation instructions, and sample code. Each package is modular, so you can use them independently or together.
Installation
All packages are (or will be) published to NuGet.org under the WE. prefix. Install via CLI or VS NuGet Manager:
Bash
dotnet add package WE.GlobalExceptionHandler --version 1.0.0-preview.1
dotnet add package WE.CQRS --version 1.0.0-preview.1
dotnet add package WE.SerilogSetup --version 1.0.0-preview.1
dotnet add package WE.EfCoreHelpers --version 1.0.0-preview.1
dotnet add package WE.RedisCache --version 1.0.0-preview.1
For local testing (from your nupkgs folder):
Bash
dotnet nuget add source ../nupkgs --name LocalWE
dotnet add package WE.GlobalExceptionHandler --source ../nupkgs
License: MIT. Source: GitHub Repo (replace with your URL).
1. WE.GlobalExceptionHandler
Features
•	Catches unhandled exceptions globally in ASP.NET Core pipeline.
•	Returns standardized ProblemDetails JSON (RFC 7807) with title, detail, status, traceId.
•	Maps exception types to appropriate HTTP status codes (e.g., 400 for ArgumentException, 401 for Unauthorized, 404 for NotFound, 409 for Conflict, 500 default).
•	Environment-aware: Full details/stack trace in Development; redacted in Production.
•	Logging integration: Errors logged with traceId.
Explanation
This middleware sits early in the pipeline to handle exceptions before they bubble up. It uses pattern matching to determine status codes, making responses more RESTful and user-friendly. Custom exceptions can be mapped via options (future enhancement).
Sample Usage
In Program.cs:
C#
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Add middleware (one line)
app.UseWEGlobalExceptionHandler();

app.Run();
Example Response (for a NotFoundException in Prod):
JSON
{
  "type": "https://httpstatuses.com/404",
  "title": "Not Found",
  "status": 404,
  "detail": "An error occurred while processing your request.",
  "instance": "/api/products/123",
  "traceId": "0HM123ABC456DEF"
}
In Dev, adds exception and stackTrace extensions.
2. WE.CQRS
Features
•	Simplified CQRS using MediatR: Automatic handler registration via assembly scanning.
•	Built-in pipeline behaviors: Logging (request start/end) and Validation (FluentValidation integration).
•	Supports commands/queries with async handling.
•	Scans calling assembly by default; optional explicit assemblies.
Explanation
CQRS separates reads (queries) from writes (commands). We wrap MediatR for minimal setup: one call registers everything. Behaviors run automatically (log + validate before handler executes).
Dependencies: MediatR, FluentValidation.
Sample Usage
In Program.cs:
C#
builder.Services.AddWECQRS();  // One line: scans current assembly
// Or: builder.Services.AddWECQRS(typeof(MyHandler).Assembly);
Define a Query/Command (in your app):
C#
public record GetProductQuery(Guid Id) : IRequest<ProductDto>;

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken ct)
    {
        // Fetch from DB
        return new ProductDto { Id = request.Id, Name = "Sample" };
    }
}

public class GetProductQueryValidator : AbstractValidator<GetProductQuery>
{
    public GetProductQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
Usage in Controller/Endpoint:
C#
private readonly ISender _sender;  // Inject

public async Task<IActionResult> GetProduct(Guid id)
{
    var result = await _sender.Send(new GetProductQuery(id));
    return Ok(result);
}
Validation throws if invalid; logs handle start/end.
3. WE.SerilogSetup
Features
•	Opinionated Serilog configuration: Console + rolling file sinks.
•	Enrichers: LogContext, MachineName, ThreadId, ProcessId, EnvironmentName.
•	Reads from appsettings.json (Serilog section).
•	Supports request logging middleware (for HTTP details).
Explanation
Serilog is a structured logger. We provide defaults for production (JSON-friendly output, rolling files) while allowing config overrides.
Dependencies: Serilog.AspNetCore, enrichers.
Sample Usage
In Program.cs:
C#
builder.Host.UseWESerilog();  // One line

// Optional: add request logging in pipeline
app.UseSerilogRequestLogging();
appsettings.json (optional override):
JSON
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/app-.log", "rollingInterval": "Day" } }
    ]
  }
}
Logs include structured properties like machine name, thread ID.
4. WE.EfCoreHelpers
Features
•	Simplified DbContext registration (SQL Server with retry).
•	Auditing: Auto Created/Updated timestamps + user + IP (via interfaces).
•	Soft-delete: Converts Delete to Update (sets IsDeleted/DeletedAt); query filters.
•	Multi-tenancy: Optional row-level isolation via TenantId filter.
•	Pooling: Optional DbContext pooling for perf.
•	Conventions: Snake_case names, UTC DateTime converter.
•	Helpers: Undelete, IncludeDeleted queries.
•	Health check: Auto DbContext connectivity check.
Explanation
Wraps common EF Core boilerplate. Interfaces (IAuditable, ISoftDeletable, IMultiTenantEntity) trigger auto behavior. Interceptor handles save changes; conventions applied in OnModelCreating.
Dependencies: Microsoft.EntityFrameworkCore.SqlServer, HealthChecks.EntityFrameworkCore.
Sample Usage
In Program.cs:
C#
builder.Services.AddWEDbContext<AppDbContext>(
    builder.Configuration.GetConnectionString("Default"),
    enableMultiTenancy: true,
    enablePooling: true,
    poolSize: 256);
In AppDbContext.cs:
C#
public class AppDbContext : DbContext
{
    private readonly ITenantAccessor? _tenantAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantAccessor tenantAccessor) : base(options)
    {
        _tenantAccessor = tenantAccessor;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyWEConventions(_tenantAccessor);
    }
}
Entity Example:
C#
public class Product : IAuditable, ISoftDeletable, IMultiTenantEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    // ... other properties

    // Interfaces auto-add fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string? UpdatedByIp { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid TenantId { get; set; }
}
Undelete Helper:
C#
var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);
context.Undelete(product);
await context.SaveChangesAsync();
Include Deleted:
C#
var allProducts = await context.Products.IncludeDeleted().ToListAsync();
Health endpoint (/health) will show "efcore-db" status.
5. WE.RedisCache
Features
•	Simplified distributed cache registration (StackExchange.Redis).
•	Typed Get/Set extensions with JSON serialization + default expirations.
•	Auto health check for Redis connectivity.
Explanation
Wraps IDistributedCache for Redis. Typed methods hide byte[]/string conversions. Health check probes connection.
Dependencies: Microsoft.Extensions.Caching.StackExchangeRedis, HealthChecks.Redis.
Sample Usage
In Program.cs:
C#
builder.Services.AddWERedisCache(builder.Configuration.GetConnectionString("Redis"));
Usage (inject IDistributedCache):
C#
var cache = serviceProvider.GetRequiredService<IDistributedCache>();

// Set typed
await cache.SetAsync("product:123", new ProductDto { Name = "Sample" });

// Get typed
var product = await cache.GetAsync<ProductDto>("product:123");
Health endpoint shows "redis-cache" status.
