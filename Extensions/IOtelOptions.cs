namespace HetBeginstation.Common;

/// <summary>
/// Options for configuring OpenTelemetry auto-registration.
/// </summary>
public sealed class OtelOptions
{
    /// <summary>
    /// Service name override. Defaults to <c>Assembly.GetEntryAssembly()?.GetName().Name</c>.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// OTLP endpoint (base address). Defaults to <c>http://localhost:4317</c>.
    /// Set via <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable or override here.
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// OTLP headers dictionary, e.g., for authentication.
    /// </summary>
    public Dictionary<string, string> OtlpHeaders { get; set; } = new();

    /// <summary>
    /// Whether to include EF Core command text in spans.
    /// Note: The EF Core instrumentation beta no longer supports <c>SetDbStatementForText</c>.
    /// Use the experimental <c>OTEL_DOTNET_EXPERIMENTAL_EFCORE_ENABLE_TRACE_DB_QUERY_PARAMETERS</c>
    /// environment variable instead.
    /// </summary>
    public bool IncludeEfCommandText { get; set; } = false;

    /// <summary>
    /// Whether to enable metric collection. Defaults to <c>true</c>.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Whether to enable trace collection. Defaults to <c>true</c>.
    /// </summary>
    public bool EnableTracing { get; set; } = true;
}
