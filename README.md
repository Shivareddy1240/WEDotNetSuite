\# WE DotNet Suite



Collection of lightweight, opinionated .NET libraries that make common setups ridiculously simple.



Philosophy: Install → 1–2 lines of code → done.



\## Packages



| Package                        | Purpose                              | One-liner example                              |

|--------------------------------|--------------------------------------|------------------------------------------------|

| WE.GlobalExceptionHandler      | Global error handling middleware     | `app.UseWEGlobalExceptionHandler();`           |

| WE.CQRS                        | Simplified CQRS with MediatR         | `services.AddWECQRS();`                        |

| WE.SerilogSetup                | Sensible Serilog configuration       | `builder.Host.UseWESerilog();`                 |

| WE.EfCoreHelpers               | EF Core registration helpers         | (coming soon)                                  |

| WE.RedisCache                  | Redis distributed cache + health     | `services.AddWERedisCache(...);`               |



\## Getting Started



Clone → open in VS/Rider → `dotnet build`



To test locally:

```bash

cd WE.GlobalExceptionHandler

dotnet pack -c Release -o ../../nupkgs

