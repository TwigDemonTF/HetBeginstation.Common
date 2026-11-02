using HetBeginstation.Common.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace HetBeginstation.Common.Extensions;

/// <summary>
/// Extension methods for registering OpenTelemetry with minimal configuration.
/// Call <see cref="AddOtel(IServiceCollection, OtelOptions?)"/> early in your
/// host builder pipeline to set up tracing, metrics, and logging.
/// </summary>
public static class OtelExtensions
{
    /// <summary>
    /// Registers OpenTelemetry tracing, metrics, and logging with sensible defaults for
    /// an event-driven microservice architecture.
    /// <para>
    /// Auto-configured instrumentation includes:
    /// <list type="bullet">
    ///   <item><description>ASP.NET Core (incoming HTTP)</description></item>
    ///   <item><description>HttpClient (outgoing HTTP)</description></item>
    ///   <item><description>gRPC client</description></item>
    ///   <item><description>Entity Framework Core (traces)</description></item>
    ///   <item><description>RabbitMQ client (7.x built-in)</description></item>
    ///   <item><description>.NET Runtime &amp; process metrics</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// OTLP exporter sends to <c>http://localhost:4317</c> by default.
    /// Override via <paramref name="configure"/> or <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>.
    /// </para>
    /// </summary>
    public static void AddOtel(this IServiceCollection services, OtelOptions? configure = null)
    {
        var options = configure ?? new OtelOptions();

        services
            .AddOpenTelemetry()
            .ConfigureResource(builder => OtelResourceFactory.Configure(builder, options))
            .WithTracing(b =>
            {
                if (options.EnableTracing)
                    TraceConfigurator.Configure(b, options);
            })
            .WithMetrics(b =>
            {
                if (options.EnableMetrics)
                    MetricConfigurator.Configure(b, options);
            })
            .WithLogging(b => LoggingConfigurator.Configure(b, options));
    }
}
