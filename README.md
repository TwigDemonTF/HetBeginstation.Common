# HetBeginstation.Common

Shared C# library that provides **OpenTelemetry auto-configuration** for event-based microservice architectures.

## Quick Start

```csharp
using HetBeginstation.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

// One line enables tracing, metrics, and logging with OTLP export
builder.Services.AddOtel();

var app = builder.Build();
app.Run();
```

That's it. The following instrumentation is registered automatically:

| Signal | Instrumentation |
|--------|----------------|
| **Traces** | ASP.NET Core, HttpClient, gRPC, EF Core, RabbitMQ |
| **Metrics** | ASP.NET Core, HttpClient, Runtime, Process |
| **Logs** | ILogger → OTLP |

All three signals export via **OTLP** to `http://localhost:4317` by default.

## Configuration

### With defaults (recommended)

```csharp
builder.Services.AddOtel();
```

### With options

```csharp
builder.Services.AddOtel(new OtelOptions
{
    ServiceName = "OrderService",
    OtlpEndpoint = "http://otel-collector:4317",
    OtlpHeaders = new() { ["Authorization"] = "Bearer my-token" },
    EnableTracing = true,
    EnableMetrics = true,
});
```

### Options reference

| Property | Default | Description |
|----------|---------|-------------|
| `ServiceName` | Entry assembly name | Override the service name in traces/metrics/logs |
| `OtlpEndpoint` | `http://localhost:4317` | OTLP collector endpoint |
| `OtlpHeaders` | `{}` | Headers for OTLP auth (e.g., API keys) |
| `EnableTracing` | `true` | Enable distributed tracing |
| `EnableMetrics` | `true` | Enable metric collection |

### Environment variables

The package respects standard OpenTelemetry environment variables:

| Variable | Effect |
|----------|--------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP endpoint URL |
| `OTEL_SERVICE_NAME` | Service name override |
| `ASPNETCORE_ENVIRONMENT` | Affects sampling (always-on in Development/Testing) |

## RabbitMQ Integration

### Publishing with semantic attributes

```csharp
using HetBeginstation.Common.RabbitMQ;

// channel is a RabbitMQ.Client.IChannel (7.x)
await channel.PublishAsync(
    exchange: "orders",
    routingKey: "order.created",
    body: bodyBytes,
    eventType: "OrderCreated",
    correlationId: message.CorrelationId,
    messageId: message.MessageId,
    aggregateType: "Order",
    aggregateId: message.OrderId
);
```

### Consuming with semantic attributes

```csharp
using HetBeginstation.Common.RabbitMQ;

// channel is a RabbitMQ.Client.IChannel (7.x)
var consumer = RabbitMQExtensions.CreateTrackedConsumer(channel,
    async (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body, ct) =>
    {
        // The current Activity span is enriched with routing key,
        // exchange, correlation ID, event type, etc. from message headers.
        await ProcessMessage(body, properties);
    });

await channel.BasicConsumeAsync(
    queue: "order.created",
    autoAck: true,
    consumer: consumer);
```

### Manual trace context propagation

```csharp
using HetBeginstation.Common.RabbitMQ;

var props = new BasicProperties();
props.InjectTraceContext(); // Injects W3C traceparent from current Activity

// Later, when consuming:
var traceparent = RabbitMQExtensions.ExtractTraceparent(properties);
```

### Semantic conventions for events

| Attribute | Description |
|-----------|-------------|
| `event.type` | Business event name (e.g., "OrderCreated") |
| `event.correlation_id` | Cross-cutting correlation ID |
| `event.aggregate_id` | Domain aggregate identifier |
| `event.aggregate_type` | Domain aggregate type (e.g., "Order") |
| `event.message_id` | Message envelope ID |
| `event.retry_count` | Redelivery attempt count |
| `rabbitmq.exchange` | RabbitMQ exchange name |
| `rabbitmq.routing_key` | RabbitMQ routing key |

## HttpClient tracing

Named/typed HTTP clients created via `IHttpClientFactory` are automatically
instrumented through the global HttpClient tracer registered by `AddOtel()`.

To add custom span enrichment to a specific client:

```csharp
builder.Services.AddHttpClient("PaymentClient", c =>
{
    c.BaseAddress = new Uri("https://payments-api");
}).WithTracing(activity =>
{
    activity.SetTag("payment.client.version", "2.0");
});
```

## Manual activity helpers

For scenarios that fall outside automatic instrumentation:

```csharp
using HetBeginstation.Common.Helpers;

// Start a handler activity
using var activity = ActivityContextExtensions
    .CreateHandlerActivity("OrderValidator", message);

try
{
    // ... process message ...
}
catch (Exception ex)
{
    ActivityContextExtensions.SetErrorStatus(ex);
    throw;
}

// Get current traceparent for manual propagation
var traceparent = ActivityContextExtensions.GetCurrentTraceparent();

// Enrich the current activity with event attributes
ActivityContextExtensions.SetEventAttributes(
    eventType: "OrderValidated",
    correlationId: message.CorrelationId,
    aggregateType: "Order",
    aggregateId: message.OrderId
);
```

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Your Microservice                      │
│                                                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌────────┐  │
│  │ ASP.NET  │  │ HttpClient│  │ EF Core  │  │RabbitMQ│  │
│  │ Core     │  │          │  │          │  │ Client │  │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └───┬────┘  │
│       │              │              │            │       │
│  ┌────▼──────────────▼──────────────▼────────────▼─────┐ │
│  │           OpenTelemetry SDK (auto-config)            │ │
│  │  Traces · Metrics · Logs  ──►  OTLP Exporter        │ │
│  └──────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼ (OTLP/gRPC)
               ┌──────────────────────┐
               │  OTLP Collector      │
               │  (Jaeger, Tempo,     │
               │   Grafana Cloud…)    │
               └──────────────────────┘
```

## Resource detection

The package auto-detects:

- **Service name & version** from entry assembly
- **Container ID** from `/proc/self/cgroup` (Docker)
- **Deployment environment** from `ASPNETCORE_ENVIRONMENT`
- **Host name** from `Environment.MachineName`
- **Telemetry SDK** version info

These appear as resource attributes on every trace, metric, and log record.

## Installation

1. Configure the NuGet source

```fish
dotnet nuget add source \
    "https://nuget.pkg.github.com/YOUR-USERNAME/index.json" \
    --name github \
    --username YOUR-USERNAME \
    --password "$GITHUB_PACKAGES_TOKEN" \
    --store-password-in-clear-text
```

2. Install the package

```bash
dotnet add package HetBeginstation.Common --version 1.0.1
```

## Publishing a new version

```bash
dotnet pack --configuration Release
dotnet nuget push \
    "bin/Release/HetBeginstation.Common.1.0.1.nupkg" \
    --source github \
    --api-key "$GITHUB_PACKAGES_TOKEN"
```

## Requirements

- .NET 10.0+
- A running OTLP collector (e.g., Jaeger, Grafana Tempo, Grafana Cloud)
- RabbitMQ.Client 7.x for RabbitMQ instrumentation
- Entity Framework Core for database instrumentation
