# SharedOpenTelemetry

A small .NET 8 NuGet package that gives ASP.NET Core microservices the same OpenTelemetry defaults for logs, traces, and metrics.

## Included by default

- Service resource metadata (`service.name` and optional `service.version`)
- Incoming ASP.NET Core request traces and metrics
- Outgoing `HttpClient` traces and metrics
- .NET runtime metrics
- Structured `ILogger` logs with scopes and formatted state
- OTLP export for logs, traces, and metrics

The package does **not** automatically instrument RabbitMQ messages. Add message spans and propagate trace context in the RabbitMQ publisher/consumer code (or a suitable RabbitMQ instrumentation library) separately.

## Use in a service

Install the package from your internal NuGet feed, or reference the project while developing:

```bash
dotnet add reference ../SharedOpenTelemetry/src/SharedOpenTelemetry/SharedOpenTelemetry.csproj
```

Then in `Program.cs`:

```csharp
using SharedOpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedOpenTelemetry(options =>
{
    options.ServiceName = "product-inventory";
    options.ServiceVersion = "1.0.0";
    options.ActivitySourceNames = ["ProductInventory"];
});

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

The service name should be unique per microservice. If the service already uses an `ActivitySource` for custom spans, include that source's name in `ActivitySourceNames`.

## Configuration

You can use `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "ServiceName": "product-inventory",
    "ServiceVersion": "1.0.0",
    "EnableOtlpExporter": true
  }
}
```

`OpenTelemetry:ServiceName` falls back to the `OTEL_SERVICE_NAME` environment variable, then the ASP.NET Core application name. The standard OpenTelemetry exporter environment variables are also supported by the OTLP exporter, including `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_HEADERS`.

For local development without an OTLP Collector, disable exporting:

```json
{
  "OpenTelemetry": {
    "EnableOtlpExporter": false
  }
}
```

When exporting is enabled, make sure an OTLP endpoint is available. For example, set `OTEL_EXPORTER_OTLP_ENDPOINT` to your collector endpoint. The endpoint and protocol must match your collector configuration.

## Build a NuGet package

From this directory:

```bash
dotnet pack src/SharedOpenTelemetry/SharedOpenTelemetry.csproj -c Release -o ./artifacts
```

The `.nupkg` will be placed in `artifacts/`. Publish it to your internal NuGet feed, then install it in each service with:

```bash
dotnet add package SharedOpenTelemetry --version 1.0.0
```

## Development

### Prerequisites

- .NET 8 SDK
- Visual Studio or VS Code with C# extensions

### Building

To build the package:

```bash
dotnet build src/SharedOpenTelemetry/SharedOpenTelemetry.csproj
```

To create a NuGet package:

```bash
dotnet pack src/SharedOpenTelemetry/SharedOpenTelemetry.csproj -c Release -o ./artifacts
```

### Testing

Unit tests are located in the test project. Run tests with:

```bash
dotnet test
```

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure all tests pass
5. Submit a pull request

### Dependencies

The package depends on the following OpenTelemetry libraries:
- OpenTelemetry.Extensions.Hosting
- OpenTelemetry.Exporter.OpenTelemetryProtocol
- OpenTelemetry.Instrumentation.AspNetCore
- OpenTelemetry.Instrumentation.Http
- OpenTelemetry.Instrumentation.Runtime
- RabbitMQ.Client.OpenTelemetry

## Important notes

- This package configures telemetry collection and export; you still need a backend/collector to store and query it.
- Logs are collected through the normal `Microsoft.Extensions.Logging` pipeline. Continue using `ILogger<T>` in services.
- Avoid logging passwords, tokens, personal data, or other secrets.
- The package is intentionally small. Add database, messaging, and domain-specific instrumentation only when the services need it.
