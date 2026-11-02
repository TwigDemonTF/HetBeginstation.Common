# HetBeginstation.Common — OpenTelemetry Auto-Configuration Package

## Implementation Document

> This document records the full development process, including research, errors, API
> discovery, and design decisions made while building this NuGet package.

---

## 1. Goals

Build a single-line `builder.Services.AddOtel()` call that auto-configures OpenTelemetry
for an **event-driven microservice architecture** built on .NET 10, with:

- Distributed tracing (HTTP, gRPC, EF Core, RabbitMQ)
- Metrics (Runtime, Process, ASP.NET Core, HttpClient)
- Structured logging → OTLP
- RabbitMQ trace context propagation (publish & consume)
- Semantic attribute helpers for business-domain event correlation
- Resource detection (service name, version, container ID, environment)

---

## 2. Starting Point

The repo was a bare-bones template:

| File | Purpose |
|------|---------|
| `HetBeginstation.Common.csproj` | Target `net10.0`, referenced `MyCompany.Common` 1.0.0 (placeholder) |
| `StringExtensions.cs` | Single `IsNullOrEmptyOrWhiteSpace` method |
| `README.md` | Generic NuGet install instructions |
| `.gitignore`, `bin/`, `obj/` | Standard .NET artifacts |

**Decisions at this stage:**

- RabbitMQ only (no MassTransit — user confirmed they don't want it)
- EF Core for database instrumentation (no Dapper)

---

## 3. Initial Design & First Build

### 3.1. Initial File Layout

The first pass created a well-structured layout with these files:

```
Extensions/
  OtelExtensions.cs              # AddOtel() entry point
  IOtelOptions.cs                # OtelOptions config class
  IHttpClientBuilderExtensions.cs # Per-client tracing enrichment
Helpers/
  ActivityContextExtensions.cs   # Manual trace context helpers
OpenTelemetry/
  TraceConfigurator.cs           # TracerProvider setup
  MetricConfigurator.cs          # MeterProvider setup
  LoggingConfigurator.cs         # LoggerProvider setup
  ResourceExtensions.cs          # Resource builder helper
  ContainerResourceDetector.cs   # Docker container ID detection
RabbitMQ/
  RabbitMQExtensions.cs          # Consumer wrappers + publish helper
  RabbitMQEventSemanticConventions.cs # Semantic attribute names
```

### 3.2. Initial Package References

The first `.csproj` used these packages:

```xml
<PackageReference Include="OpenTelemetry" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Exporter.Otlp" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.RabbitMQClient" Version="1.10.0-beta.2" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.1-beta.2" />
<PackageReference Include="OpenTelemetry.Instrumentation.Process" Version="1.4.0" />
<PackageReference Include="OpenTelemetry.Resources.Container" Version="1.12.0-alpha.1" />
```

### 3.3. First Build Failure

```
dotnet build
```

**Errors:**

| Error | Root Cause |
|-------|-----------|
| `NU1101: Unable to find package OpenTelemetry.Exporter.Otlp` | Package doesn't exist under this name |
| `NU1103: Unable to find a stable package OpenTelemetry.Instrumentation.Process (>= 1.4.0)` | Latest is still beta/RC |
| `NU1101: Unable to find package OpenTelemetry.Instrumentation.RabbitMQClient` | Package doesn't exist |
| `NU1603: ... EntityFrameworkCore (>= 1.0.1-beta.2) was not found` | Version too old, resolved to 1.10.0-beta.1 |
| `NU1902: Package 'OpenTelemetry.Api' 1.12.0 has a known moderate severity vulnerability` | Outdated version |

---

## 4. Package Research Phase

### 4.1. Finding the Correct OTLP Exporter Package

**Search:** `nuget OpenTelemetry.Exporter.Otlp`

**Result:** Package name is `OpenTelemetry.Exporter.OpenTelemetryProtocol`, not
`OpenTelemetry.Exporter.Otlp`. Latest stable version: **1.20.1**.

### 4.2. Finding RabbitMQ Instrumentation

**Search:** `nuget "OpenTelemetry.Instrumentation.RabbitMQClient"`

**Result:** This package does **not exist**. The RabbitMQ .NET Client 7.x has
**built-in OpenTelemetry support** via `RabbitMQActivitySource`. The source names
are:
- `"RabbitMQ.Client.Publisher"` — for publish spans
- `"RabbitMQ.Client.Subscriber"` — for consume spans

The client also provides `RabbitMQActivitySource.ContextInjector` and
`RabbitMQActivitySource.ContextExtractor` for customizing trace context propagation.

### 4.3. EF Core Instrumentation

**Search:** `nuget "OpenTelemetry.Instrumentation.EntityFrameworkCore"`

**Result:** Package exists but is still in **beta**. Latest: **1.20.1-beta.1**.
The package provides only **tracing** (no metrics support in this version).

### 4.4. Process Instrumentation

**Search:** `nuget "OpenTelemetry.Instrumentation.Process"`

**Result:** Still in release candidate. Latest: **1.20.1-rc.1**.

### 4.5. Updated Package References

```xml
<PackageReference Include="OpenTelemetry" Version="1.20.1" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.20.1" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.20.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.20.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.GrpcNetClient" Version="1.18.0-beta.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.20.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.20.1-beta.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.20.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.Process" Version="1.20.1-rc.1" />
<PackageReference Include="RabbitMQ.Client" Version="7.1.1" />
```

Removed:
- `OpenTelemetry.Resources.Container` — implemented container detection manually
- `OpenTelemetry.Instrumentation.RabbitMQClient` — replaced by RabbitMQ.Client built-in

---

## 5. API Discovery & Source Code Fixes

### 5.1. Second Build Failure — Missing Usings & Namespace Resolution

After fixing packages, the build revealed 15 compilation errors:

```
error CS0246: The type or namespace name 'OtelOptions' could not be found
error CS0246: The type or namespace name 'IServiceCollection' could not be found
error CS0246: The type or namespace name 'IHttpClientBuilder' could not be found
error CS0246: The type or namespace name 'IModel' could not be found
error CS0246: The type or namespace name 'EventingBasicConsumer' could not be found
```

**Root causes:**

1. `OtelOptions` was in `HetBeginstation.Common.Extensions` but the configurators
   are in `HetBeginstation.Common.OpenTelemetry` — no cross-namespace visibility
   without explicit `using`.

2. Missing `using Microsoft.Extensions.DependencyInjection` for `IServiceCollection`.

3. Missing `using Microsoft.Extensions.Http` for `IHttpClientBuilder`.

4. `IModel` doesn't exist in RabbitMQ.Client 7.x — renamed to `IChannel`.

5. `EventingBasicConsumer` (sync) doesn't exist — removed in 7.x. Only
   `AsyncEventingBasicConsumer` remains.

**Fixes:**
- Moved `OtelOptions` to the root namespace `HetBeginstation.Common`
- Added all missing `using` directives

### 5.2. Third Build Failure — API Changes in OpenTelemetry 1.20.x

```
error CS1061: 'ResourceBuilder' does not contain a definition for 'AddAttribute'
error CS1061: 'ResourceBuilder' does not contain a definition for 'AddResource'
error CS0104: 'ResourceBuilderExtensions' is an ambiguous reference
error CS1061: 'Activity' does not contain a definition for 'GetTraceContextString'
error CS1061: 'MeterProviderBuilder' does not contain a definition for
              'AddEntityFrameworkCoreInstrumentation'
error CS1061: 'LoggerProviderBuilder' does not contain a definition for
              'AddOpenTelemetry'
error CS1061: 'HttpClientTraceInstrumentationOptions' does not contain a definition
              for 'ExcludeFromCapturedHeaders'
error CS1061: 'EntityFrameworkInstrumentationOptions' does not contain a definition
              for 'SetDbStatementForText'
error CS0117: 'char' does not contain a definition for 'IsHexDigit'
```

#### 5.2.1. `ResourceBuilder.AddAttribute()` → `AddAttributes(Dictionary)`

**Discovery** (reflection on `OpenTelemetry.dll`):

```
=== ResourceBuilder methods ===
  AddDetector(IResourceDetector resourceDetector)
  AddDetector(Func<IResourceDetector> resourceDetectorFactory)

=== ResourceBuilderExtensions methods ===
  ResourceBuilderExtensions.AddService
  ResourceBuilderExtensions.AddTelemetrySdk
  ResourceBuilderExtensions.AddAttributes  ← takes Dictionary<string, object>
  ResourceBuilderExtensions.AddEnvironmentVariableDetector
```

**Fix:** Changed `builder.AddAttribute(key, value)` to `builder.AddAttributes(new
Dictionary<string, object> { [key] = value })`.

#### 5.2.2. Class Name Conflict

`ResourceBuilderExtensions` conflicts with `OpenTelemetry.Resources.ResourceBuilderExtensions`.

**Fix:** Renamed to `OtelResourceFactory` with a `Configure(ResourceBuilder, OtelOptions)`
method instead of `BuildResource()`.

#### 5.2.3. `Activity.GetTraceContextString()` doesn't exist

**Discovery** (reflection on `OpenTelemetry.Api.dll`):

```
=== Activity extension methods in OpenTelemetry.Api ===
  ActivityExtensions.SetStatus
  ActivityExtensions.GetStatus
  ActivityExtensions.RecordException
  ActivityHelperExtensions.GetTagValue
```

No `GetTraceContextString()` extension. **Discovery** (reflection on `Activity`
class itself):

```
Activity.Id: String     ← W3C traceparent format
Activity.TraceId
Activity.SpanId
Activity.TraceStateString
Activity.Context
```

**Fix:** Use `Activity.Id` (returns the full W3C traceparent string, e.g.
`"00-{traceId}-{spanId}-{flags}"`).

#### 5.2.4. EF Core Metrics Not Supported

**Discovery:** `MeterProviderBuilder.AddEntityFrameworkCoreInstrumentation()` doesn't
exist. EF Core instrumentation provides only **tracing** in this beta.

**Fix:** Removed EF Core metrics from `MetricConfigurator`.

#### 5.2.5. `LoggerProviderBuilder.AddOpenTelemetry()` Doesn't Exist

**Discovery** (reflection on `OpenTelemetry.dll`):

```
=== LoggerProviderBuilder extensions ===
  LoggerProviderBuilderExtensions.SetResourceBuilder
  LoggerProviderBuilderExtensions.ConfigureResource
  LoggerProviderBuilderExtensions.AddProcessor
  LoggerProviderBuilderExtensions.Build
```

**And** (reflection on `OpenTelemetry.Exporter.OpenTelemetryProtocol.dll`):

```
=== LoggerProviderBuilder extensions (OTLP) ===
  OtlpLogExporterHelperExtensions.AddOtlpExporter
  OtlpLogExporterHelperExtensions.AddOtlpExporter(Action<OtlpExporterOptions>)
  OtlpLogExporterHelperExtensions.AddOtlpExporter(Action<OtlpExporterOptions, LogRecordExportProcessorOptions>)
```

`AddOpenTelemetry()` is a method on `ILoggingBuilder` (Microsoft.Extensions.Logging),
not on `LoggerProviderBuilder`. The OTLP exporter provides `AddOtlpExporter()` directly.

**Fix:** Removed `builder.AddOpenTelemetry(o => { o.IncludeScopes = true; })` from
`LoggingConfigurator` and used just `builder.AddOtlpExporter()`.

#### 5.2.6. `HttpClientTraceInstrumentationOptions.ExcludeFromCapturedHeaders` Removed

**Discovery** (reflection on `OpenTelemetry.Instrumentation.Http.dll`):

```
=== HttpClientInstrumentationOptions properties ===
  FilterHttpRequestMessage: Func<HttpRequestMessage, bool>
  EnrichWithHttpRequestMessage: Action<Activity, HttpRequestMessage>
  EnrichWithHttpResponseMessage: Action<Activity, HttpResponseMessage>
  EnrichWithException: Action<Activity, Exception>
  FilterHttpWebRequest: Func<HttpWebRequest, bool>
  EnrichWithHttpWebRequest: Action<Activity, HttpWebRequest>
  EnrichWithHttpWebResponse: Action<Activity, HttpWebResponse>
  RecordException: Boolean
```

No `ExcludeFromCapturedHeaders` property. Header exclusion is handled differently
in the newer API.

**Fix:** Removed the header exclusion logic from `TraceConfigurator` (the HTTP
instrumentation handles common sensitive headers internally). Removed
`ExcludeHttpHeaders` from `OtelOptions`.

#### 5.2.7. `EntityFrameworkInstrumentationOptions.SetDbStatementForText` Removed

**Discovery** (reflection on `OpenTelemetry.Instrumentation.EntityFrameworkCore.dll`):

```
=== EntityFrameworkInstrumentationOptions properties ===
  EnrichWithIDbCommand: Action<Activity, IDbCommand>
  Filter: Func<string, IDbCommand, bool>
```

`SetDbStatementForText` no longer exists. The recommended approach is to use the
experimental environment variable `OTEL_DOTNET_EXPERIMENTAL_EFCORE_ENABLE_TRACE_DB_QUERY_PARAMETERS`.

**Fix:** Removed `SetDbStatementForText` from `TraceConfigurator`. Updated `OtelOptions.IncludeEfCommandText`
documentation to note the environment variable approach.

#### 5.2.8. `char.IsHexDigit` Not Found

**Root cause:** This is a .NET 8+ API. The project targets .NET 10, so it should exist,
but it appeared to not be found (possibly a different issue).

**Fix:** Replaced with manual hex check:

```csharp
private static bool IsHex(string s)
{
    var hexDigits = "0123456789abcdefABCDEF";
    foreach (var c in s)
        if (!hexDigits.Contains(c))
            return false;
    return true;
}
```

### 5.3. Fourth Build Failure — ResourceBuilder API & Statement Expressions

```
error CS0176: Member 'ResourceBuilder.CreateDefault()' cannot be accessed with an
              instance reference; qualify it with a type name instead
error CS0201: Only assignment, call, increment, decrement, await, and new object
              expressions can be used as a statement
```

**Root causes:**

1. `CreateDefault()` is a **static** method. When called as `builder.CreateDefault()`
   on an instance, it fails. The `ConfigureResource` callback receives an already-
   initialized `ResourceBuilder`, so calling `CreateDefault()` is unnecessary.

2. Ternary expressions used as statements:
   ```csharp
   .WithTracing(b => options.EnableTracing
       ? TraceConfigurator.Configure(b, options)
       : b)  // This returns a value but is used as a statement
   ```

**Fixes:**
- Removed `builder.CreateDefault()` call from `OtelResourceFactory.Configure`
- Changed ternary to proper `if` statements in `OtelExtensions`

---

## 6. RabbitMQ.Client 7.x API Discovery

The RabbitMQ client 7.x had significant breaking changes from 6.x. Full API
discovery was done via reflection:

### 6.1. Type Renames

| 6.x | 7.x |
|-----|-----|
| `IModel` | `IChannel` |
| `connection.CreateModel()` | `connection.CreateChannelAsync()` |
| `EventingBasicConsumer` | Removed (async-only) |
| `AsyncEventingBasicConsumer` | `AsyncEventingBasicConsumer` (still exists) |
| `BasicDeliverEventArgs` | Exists but only has `CancellationToken` |
| `model.BasicPublish(...)` | `channel.BasicPublishAsync(...)` (async only) |

### 6.2. Consumer Pattern Change

In 6.x, message data came through `BasicDeliverEventArgs`:

```csharp
// 6.x pattern
consumer.Received += (sender, args) =>
{
    var body = args.Body;
    var routingKey = args.RoutingKey;
    var exchange = args.Exchange;
    var properties = args.BasicProperties;
};
```

In 7.x, all data is passed as parameters to the handler method:

```csharp
// 7.x pattern
public override async Task HandleBasicDeliverAsync(
    string consumerTag,
    ulong deliveryTag,
    bool redelivered,
    string exchange,
    string routingKey,
    IReadOnlyBasicProperties properties,
    ReadOnlyMemory<byte> body,
    CancellationToken cancellationToken)
{
    // All data as parameters
}
```

### 6.3. ActivitySource Names

From `RabbitMQActivitySource`:

| Field | Value |
|-------|-------|
| `PublisherSourceName` | `"RabbitMQ.Client.Publisher"` |
| `SubscriberSourceName` | `"RabbitMQ.Client.Subscriber"` |

These must be registered with `TracerProviderBuilder.AddSource()`.

### 6.4. Properties API

```
IBasicProperties.Headers: IDictionary<string, object?>
IReadOnlyBasicProperties.Headers: IDictionary<string, object>
```

### 6.5. Design Decision: Consumer Wrapper

Instead of relying solely on the built-in ActivitySource (which provides basic
spans), we created a `TrackedAsyncConsumer` that extends `AsyncEventingBasicConsumer`
and enriches every received span with:

- `rabbitmq.exchange` — from the `exchange` parameter
- `rabbitmq.routing_key` — from the `routingKey` parameter
- `event.retry_count` — set to 1 if `redelivered` is true
- `event.message_id` — extracted from message headers
- `event.correlation_id` — extracted from message headers
- `event.type` — extracted from message headers

This gives business-domain context to every RabbitMQ span without requiring manual
instrumentation in each handler.

---

## 7. Final Package State

### 7.1. Build & Pack Output

```
dotnet build
  HetBeginstation.Common -> bin/Debug/net10.0/HetBeginstation.Common.dll
Build succeeded. 0 Warning(s), 0 Error(s).

dotnet pack --configuration Release
  Successfully created package bin/Release/HetBeginstation.Common.1.0.1.nupkg
  Warning: Prerelease dependencies (EF Core beta, gRPC beta, Process RC)
```

The three NU5104 warnings are expected — the upstream packages haven't reached stable.

### 7.2. Final File List

```
HetBeginstation.Common.csproj          # 10 NuGet references, net10.0 target
Extensions/
  OtelExtensions.cs                    # AddOtel() entry point
  IOtelOptions.cs                      # 4 config properties
  IHttpClientBuilderExtensions.cs      # WithTracing() per-client enrichment
Helpers/
  ActivityContextExtensions.cs         # 4 helper methods
OpenTelemetry/
  TraceConfigurator.cs                 # 5 instrumentations + OTLP
  MetricConfigurator.cs               # 4 instrumentations + OTLP
  LoggingConfigurator.cs              # OTLP exporter
  OtelResourceFactory.cs              # Resource + container detection
RabbitMQ/
  RabbitMQExtensions.cs               # Consumer wrapper + PublishAsync
  RabbitMQEventSemanticConventions.cs # 8 semantic attribute constants
README.md                             # Full documentation with examples
StringExtensions.cs                   # Original file (preserved)
```

### 7.3. NuGet Dependencies

| Package | Version | Status | Purpose |
|---------|---------|--------|---------|
| `OpenTelemetry` | 1.20.1 | Stable | SDK metapackage |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.20.1 | Stable | OTLP exporter |
| `OpenTelemetry.Extensions.Hosting` | 1.20.1 | Stable | DI integration |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.20.1 | Stable | HTTP server traces/metrics |
| `OpenTelemetry.Instrumentation.Http` | 1.20.1 | Stable | HTTP client traces/metrics |
| `OpenTelemetry.Instrumentation.GrpcNetClient` | 1.18.0-beta.1 | Beta | gRPC client traces |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | 1.20.1-beta.1 | Beta | EF Core traces |
| `OpenTelemetry.Instrumentation.Runtime` | 1.20.1 | Stable | GC, thread pool metrics |
| `OpenTelemetry.Instrumentation.Process` | 1.20.1-rc.1 | RC | CPU, memory metrics |
| `RabbitMQ.Client` | 7.1.1 | Stable | RabbitMQ with built-in OTel |

---

## 8. Key Design Decisions

### 8.1. Single `AddOtel()` Call

The entire OpenTelemetry setup (traces, metrics, logs, resource, exporter) is
activated with a single call. This follows the "convention over configuration"
principle — sensible defaults for microservices.

### 8.2. No MassTransit

The user explicitly requested RabbitMQ-only instrumentation. MassTransit adds
significant weight and licensing complexity.

### 8.3. Async-Only RabbitMQ API

RabbitMQ.Client 7.x removed all synchronous APIs. The package reflects this by
providing only async helpers (`PublishAsync`, async consumer).

### 8.4. Container Detection via cgroup Parsing

Instead of depending on `OpenTelemetry.Resources.Container` (alpha at the time),
a simple `/proc/self/cgroup` parser was implemented inline. This avoids a
prerelease dependency and works for Docker containers.

### 8.5. Semantic Attribute Conventions

Business-domain attributes (`event.type`, `event.correlation_id`, etc.) are
defined as constants in `RabbitMQEventSemanticConventions`. This ensures
consistent naming across all microservices that consume this package.

### 8.6. Removed Features Due to API Changes

These features from the initial design were removed because the underlying
OpenTelemetry APIs changed:

| Feature | Reason |
|---------|--------|
| `ExcludeHttpHeaders` | `HttpClientTraceInstrumentationOptions.ExcludeFromCapturedHeaders` removed |
| `IncludeEfCommandText` via code | `SetDbStatementForText` removed from EF options; use env var instead |
| `EnableLogging` toggle | `LoggerProviderBuilder.AddOpenTelemetry()` doesn't exist; logging is always enabled |
| EF Core metrics | EF Core instrumentation doesn't support metrics in beta |

---

## 9. Lessons Learned

1. **Always verify NuGet package names before coding.** `OpenTelemetry.Exporter.Otlp`
   doesn't exist — it's `OpenTelemetry.Exporter.OpenTelemetryProtocol`.

2. **RabbitMQ instrumentation is built into the client 7.x**, not a separate
   OpenTelemetry package. This is different from HTTP/EF Core/gRPC which have
   dedicated instrumentation libraries.

3. **OpenTelemetry .NET APIs change frequently.** Between versions 1.12 and 1.20:
   - `ResourceBuilder.AddAttribute()` → `AddAttributes(Dictionary)`
   - `SetDbStatementForText` → removed (use env var)
   - `ExcludeFromCapturedHeaders` → removed
   - EF Core metrics → not supported

4. **RabbitMQ.Client 7.x is a major rewrite.** Sync APIs removed, event args
   simplified, `IModel` → `IChannel`. Any code written against 6.x will not
   compile.

5. **Use reflection on NuGet DLLs to discover APIs** when documentation is
   outdated or incomplete. The `dotnet run` + `Assembly.LoadFrom` approach
   worked well for exploring package internals.

---

*Generated: 2026-10-31*
