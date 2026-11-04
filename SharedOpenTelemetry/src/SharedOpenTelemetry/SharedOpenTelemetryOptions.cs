namespace SharedOpenTelemetry;

/// <summary>
/// Configuration options for the SharedOpenTelemetry package.
/// These options control the behavior of OpenTelemetry instrumentation and export.
/// </summary>
public sealed class SharedOpenTelemetryOptions
{
    /// <summary>
    /// Gets or sets the service name that will be used as the service.name resource attribute.
    /// This should be unique per microservice.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version that will be used as the service.version resource attribute.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the OTLP exporter is enabled.
    /// Set to false for local development without a collector.
    /// </summary>
    public bool EnableOtlpExporter { get; set; } = true;

    /// <summary>
    /// Gets or sets additional ActivitySource names to include for custom spans.
    /// This allows adding custom instrumentation beyond the default ASP.NET Core and HTTP instrumentation.
    /// </summary>
    public string[] ActivitySourceNames { get; set; } = [];
}
