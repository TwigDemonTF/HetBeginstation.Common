namespace SharedOpenTelemetry;

/// <summary>
/// Small set of shared defaults. Exporter endpoints can also be configured with
/// the standard OTEL_* environment variables.
/// </summary>
public sealed class SharedOpenTelemetryOptions
{
    /// <summary>The service.name resource attribute.</summary>
    public string? ServiceName { get; set; }

    /// <summary>The service.version resource attribute, if known.</summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Adds the OTLP exporter to traces, metrics, and logs.
    /// Set to false for local development without a collector.
    /// </summary>
    public bool EnableOtlpExporter { get; set; } = true;

    /// <summary>
    /// Additional ActivitySource names to include for custom spans.
    /// </summary>
    public string[] ActivitySourceNames { get; set; } = [];
}
