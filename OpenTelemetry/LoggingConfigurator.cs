using OpenTelemetry.Logs;

namespace HetBeginstation.Common.OpenTelemetry;

/// <summary>
/// Configures the <see cref="LoggerProviderBuilder"/> to send structured
/// logs through OpenTelemetry so they appear alongside traces and metrics.
/// </summary>
internal static class LoggingConfigurator
{
    /// <summary>
    /// Applies logging configuration and OTLP export.
    /// </summary>
    public static LoggerProviderBuilder Configure(
        LoggerProviderBuilder builder,
        OtelOptions options)
    {
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
