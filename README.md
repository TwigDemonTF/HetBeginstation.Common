# SharedOpenTelemetry

A small .NET 10 NuGet package that gives ASP.NET Core microservices the same OpenTelemetry defaults for logs, traces, and metrics.

## Table of Contents
1. [Installation](#installation)
2. [Usage](#usage)
3. [Configuration](#configuration)
4. [Integration Details](#integration-details)
5. [Development](#development)
6. [Build & Deployment](#build--deployment)
7. [Dependencies](#dependencies)
8. [Important Notes](#important-notes)

## Included by default
- Service resource metadata (`service.name` and optional `service.version`)
- Incoming ASP.NET Core request traces and metrics
- Outgoing `HttpClient` traces and metrics
- .NET runtime metrics
- Structured `ILogger` logs with scopes and formatted state
- OTLP export for logs, traces, and metrics

The package does **not** automatically instrument RabbitMQ messages. Add message spans and propagate trace context in the RabbitMQ publisher/consumer code (or a suitable RabbitMQ instrumentation library) separately.

## Integration Details
### RabbitMQ Instrumentation
While this package doesn't automatically instrument RabbitMQ messages, you can integrate with RabbitMQ by:
1. Using the `RabbitMQ.Client.OpenTelemetry` library
2. Adding trace context propagation to your RabbitMQ publisher/consumer code
3. Manually creating spans for message processing

### Local Development Setup
```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```
This command starts RabbitMQ with:
- AMQP protocol on port 5672
- Management UI on port 15672

Access the RabbitMQ management UI at http://localhost:15672
Default credentials: guest/guest

### Jaeger Integration
This package exports telemetry data via OTLP to Jaeger. To configure Jaeger as your OTLP endpoint:

1. Set the OTLP endpoint to your Jaeger collector:
```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger-collector:4317
```

2. Configure in appsettings.json:
```json
{
  "OpenTelemetry": {
    "EnableOtlpExporter": true,
    "OtlpExporterEndpoint": "http://jaeger-collector:4317"
  }
}
```

3. Ensure your Jaeger collector is accessible and configured properly

## Installation
This package is hosted on GitHub Packages. Install the package from your company's GitHub Packages feed, or reference the project while developing.

### For Linux, macOS, and Windows:
```bash
dotnet add reference ../SharedOpenTelemetry/src/SharedOpenTelemetry/SharedOpenTelemetry.csproj
```

### Alternative: Install via GitHub Packages
```bash
dotnet add package SharedOpenTelemetry --version 1.0.0
```

To install from GitHub Packages, you'll need to configure your NuGet credentials:
1. Create a personal access token with `read:packages` permission
2. Add the GitHub Packages source to your NuGet configuration:
```bash
dotnet nuget add source https://nuget.pkg.github.com/YOUR_ORGANIZATION/index.json --name github --username YOUR_USERNAME --password YOUR_TOKEN
```

## Usage
To use the package in your ASP.NET Core service:

1. **Add the package reference** (as described in the Installation section)
2. **Configure OpenTelemetry in Program.cs**:
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

## Development
### Prerequisites
- .NET 10 SDK
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

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure all tests pass
5. Submit a pull request

## Build & Deployment
### Creating a NuGet Package
From this directory:
```bash
dotnet pack src/SharedOpenTelemetry/SharedOpenTelemetry.csproj -c Release -o ./artifacts
```

The `.nupkg` will be placed in `artifacts/`. Publish it to your internal NuGet feed, then install it in each service with:
```bash
dotnet add package SharedOpenTelemetry --version 1.0.0
```

## Dependencies
The package depends on the following OpenTelemetry libraries:
- OpenTelemetry.Extensions.Hosting
- OpenTelemetry.Exporter.OpenTelemetryProtocol
- OpenTelemetry.Instrumentation.AspNetCore
- OpenTelemetry.Instrumentation.Http
- OpenTelemetry.Instrumentation.Runtime
- RabbitMQ.Client.OpenTelemetry

## Important Notes
- This package configures telemetry collection and export; you still need a backend/collector to store and query it.
- Logs are collected through the normal `Microsoft.Extensions.Logging` pipeline. Continue using `ILogger<T>` in services.
- Avoid logging passwords, tokens, personal data, or other secrets.
- The package is intentionally small. Add database, messaging, and domain-specific instrumentation only when the services need it.