# Development Guide

This document provides guidelines and instructions for developing and maintaining the HetBeginstation.Common project.

## Project Structure

```
HetBeginstation.Common/
├── Consumer/              # Consumer service implementation
├── Publisher/             # Publisher service implementation
├── SharedOpenTelemetry/   # Shared OpenTelemetry package
│   ├── src/               # Package source code
│   ├── test/              # Package tests
│   └── README.md          # Package documentation
└── README.md              # Repository documentation
```

## Setting up the Development Environment

### Prerequisites

- .NET 8 SDK
- Visual Studio or VS Code with C# extensions
- Git

### Getting Started

1. Clone the repository:
```bash
git clone <repository-url>
cd HetBeginstation.Common
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the solution:
```bash
dotnet build
```

## Working with the SharedOpenTelemetry Package

### Package Structure

The `SharedOpenTelemetry` package contains:
- Core OpenTelemetry configuration
- Default instrumentation for ASP.NET Core
- OTLP exporter setup
- Configuration binding

### Development Workflow

1. Make changes to the `SharedOpenTelemetry/src` directory
2. Build the package:
```bash
dotnet build src/SharedOpenTelemetry/SharedOpenTelemetry.csproj
```
3. Test your changes by referencing the built package in the Consumer or Publisher services
4. If needed, rebuild and republish the NuGet package

### Testing the Package

The package includes unit tests to verify functionality. Run tests with:
```bash
dotnet test
```

## Working with Services

### Consumer Service

The Consumer service implements a message consumer. Key files:
- `Consumer/Program.cs` - Main application entry point
- `Consumer/Consumer.csproj` - Project configuration

### Publisher Service

The Publisher service implements a message publisher. Key files:
- `Publisher/Program.cs` - Main application entry point
- `Publisher/Publisher.csproj` - Project configuration

## Code Style and Standards

### C# Coding Standards

- Follow Microsoft's C# coding conventions
- Use meaningful names for variables, methods, and classes
- Maintain consistent indentation (4 spaces)
- Include XML documentation comments for public APIs
- Keep methods small and focused on single responsibilities

### OpenTelemetry Best Practices

- Use descriptive service names that uniquely identify each microservice
- Configure appropriate `ActivitySourceNames` for custom spans
- Avoid logging sensitive information
- Configure exporters appropriately for different environments

## Versioning Strategy

The project uses semantic versioning:
- Major version: Breaking changes
- Minor version: New features, backwards compatible
- Patch version: Bug fixes, backwards compatible

## Deployment

### Publishing the Package

To publish a new version of the SharedOpenTelemetry package:

1. Update the version in `SharedOpenTelemetry/src/SharedOpenTelemetry/SharedOpenTelemetry.csproj`
2. Build and pack the package:
```bash
dotnet pack src/SharedOpenTelemetry/SharedOpenTelemetry.csproj -c Release -o ./artifacts
```
3. Publish to your internal NuGet feed

### Deploying Services

Services should reference the latest stable version of the SharedOpenTelemetry package:
```bash
dotnet add package SharedOpenTelemetry --version <new-version>
```

## Troubleshooting

### Common Issues

1. **OpenTelemetry not collecting data**: Verify that the service name is configured correctly and that exporters are enabled
2. **Trace context not propagating**: Check that RabbitMQ instrumentation is properly configured
3. **Package reference issues**: Ensure the correct version of the SharedOpenTelemetry package is referenced

### Debugging

Enable verbose logging for debugging OpenTelemetry issues:
```bash
export OTEL_LOG_LEVEL=debug
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes following the coding standards
4. Write tests for new functionality
5. Ensure all tests pass
6. Submit a pull request with a clear description

## Support

For support with development, please contact the maintainers or open an issue in the repository.