using OpenTelemetry.Trace;

namespace HetBeginstation.Common.OpenTelemetry;

/// <summary>
/// Configures the <see cref="TracerProviderBuilder"/> with all recommended
/// instrumentation for an event-driven microservice.
/// </summary>
internal static class TraceConfigurator
{
    /// <summary>
    /// Applies tracing instrumentation and OTLP export.
    /// </summary>
    public static TracerProviderBuilder Configure(
        TracerProviderBuilder builder,
        OtelOptions options)
    {
        // ── HTTP client (outgoing requests) ───────────────────────────
        builder.AddHttpClientInstrumentation();

        // ── ASP.NET Core (incoming requests) ──────────────────────────
        builder.AddAspNetCoreInstrumentation(a =>
        {
            a.Filter = (context) =>
            {
                // Skip health checks and OpenTelemetry health endpoints
                var path = context.Request.Path.Value ?? string.Empty;
                return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/otel", StringComparison.OrdinalIgnoreCase);
            };
        });

        // ── gRPC client ───────────────────────────────────────────────
        builder.AddGrpcClientInstrumentation();

        // ── Entity Framework Core ─────────────────────────────────────
        builder.AddEntityFrameworkCoreInstrumentation();

        // ── RabbitMQ client (7.x built-in ActivitySource) ─────────────
        builder.AddSource("RabbitMQ.Client.Publisher");
        builder.AddSource("RabbitMQ.Client.Subscriber");

        // ── Sampler: always record in dev/test, trace-based in prod ───
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                  ?? "Production";

        if (env.Equals("Development", StringComparison.OrdinalIgnoreCase)
            || env.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            builder.SetSampler(new AlwaysOnSampler());
        }

        // ── OTLP Exporter ─────────────────────────────────────────────
        builder.AddOtlpExporter(exporter =>
        {
            if (!string.IsNullOrEmpty(options.OtlpEndpoint))
                exporter.Endpoint = new Uri(options.OtlpEndpoint);

            foreach (var kvp in options.OtlpHeaders)
                exporter.Headers += $"{kvp.Key}: {kvp.Value}";
        });

        return builder;
    }
}
