using OpenTelemetry.Metrics;

namespace HetBeginstation.Common.OpenTelemetry;

/// <summary>
/// Configures the <see cref="MeterProviderBuilder"/> with recommended
/// built-in and framework metrics for microservices.
/// </summary>
internal static class MetricConfigurator
{
    /// <summary>
    /// Applies metric instrumentation and OTLP export.
    /// </summary>
    public static MeterProviderBuilder Configure(
        MeterProviderBuilder builder,
        OtelOptions options)
    {
        // ── .NET Runtime metrics ──────────────────────────────────────
        // GC collections, gen 0/1/2, heap size, thread pool, etc.
        builder.AddRuntimeInstrumentation();

        // ── Process metrics ───────────────────────────────────────────
        // CPU time, memory working set, process ID
        builder.AddProcessInstrumentation();

        // ── ASP.NET Core metrics ──────────────────────────────────────
        // HTTP request duration, active requests, request size
        builder.AddAspNetCoreInstrumentation();

        // ── HTTP client metrics ───────────────────────────────────────
        builder.AddHttpClientInstrumentation();

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
