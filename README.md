# HetBeginstation.Common

This repository contains the HetBeginstation.Common project, which provides shared OpenTelemetry functionality for .NET microservices.

## Repository Structure

The repository contains the following components:

- **Consumer**: An ASP.NET Core service that consumes messages
- **Publisher**: An ASP.NET Core service that publishes messages
- **SharedOpenTelemetry**: A NuGet package that provides OpenTelemetry instrumentation for ASP.NET Core services

## SharedOpenTelemetry Package

The SharedOpenTelemetry package is the core component of this repository. It provides consistent OpenTelemetry configuration across all microservices in the HetBeginstation ecosystem.

### Features

- Service resource metadata (`service.name` and optional `service.version`)
- Incoming ASP.NET Core request traces and metrics
- Outgoing `HttpClient` traces and metrics
- .NET runtime metrics
- Structured `ILogger` logs with scopes and formatted state
- OTLP export for logs, traces, and metrics

### Important Notes

The package does **not** automatically instrument RabbitMQ messages. You'll need to add message spans and propagate trace context in the RabbitMQ publisher/consumer code (or use a suitable RabbitMQ instrumentation library) separately.

## Using the Package

### Installation

Install the package from your internal NuGet feed, or reference the project while developing:

```bash
dotnet add reference ../SharedOpenTelemetry/src/SharedOpenTelemetry/SharedOpenTelemetry.csproj
```

### Usage in a Service

In your `Program.cs`:

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

### Configuration

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

Configuration options fall back to environment variables:
- `OpenTelemetry:ServiceName` falls back to `OTEL_SERVICE_NAME` environment variable, then the ASP.NET Core application name
- Standard OpenTelemetry exporter environment variables are also supported, including `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_HEADERS`

For local development without an OTLP Collector, disable exporting:

```json
{
  "OpenTelemetry": {
    "EnableOtlpExporter": false
  }
}
```

## Development Guidelines

### Setting up the Development Environment

1. Ensure you have .NET 8 SDK installed
2. Clone this repository
3. Navigate to the repository directory
4. Restore dependencies with: `dotnet restore`

### Building the Package

To build the NuGet package:

```bash
cd SharedOpenTelemetry
dotnet pack src/SharedOpenTelemetry/SharedOpenTelemetry.csproj -c Release -o ./artifacts
```

The `.nupkg` will be placed in `artifacts/`. Publish it to your internal NuGet feed, then install it in each service with:

```bash
dotnet add package SharedOpenTelemetry --version 1.0.0
```

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure all tests pass
5. Submit a pull request

### Code Structure

- **Consumer**: Implements a service that consumes messages
- **Publisher**: Implements a service that publishes messages  
- **SharedOpenTelemetry/src**: Contains the shared OpenTelemetry package source code

### Testing

Unit tests and integration tests should be written for all components. Ensure tests cover:
- OpenTelemetry configuration
- Service behavior
- Message handling
- Error scenarios

### Deployment

The package should be published to an internal NuGet feed for use across services. Each service should reference the latest stable version of the package.

## Troubleshooting

### Common Issues

1. **OpenTelemetry not exporting**: Verify that `OTEL_EXPORTER_OTLP_ENDPOINT` is set correctly
2. **Missing service name**: Ensure `OpenTelemetry:ServiceName` is configured properly
3. **No trace context propagation**: Make sure RabbitMQ instrumentation is properly configured

### Debugging

Enable verbose logging to troubleshoot OpenTelemetry issues:
- Set `OTEL_LOG_LEVEL=debug`
- Check that your OTLP endpoint is accessible
- Verify that the correct environment variables are set

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support, please open an issue in the repository or contact the maintainers.