using OpenTelemetry.Resources;

namespace HetBeginstation.Common.OpenTelemetry;

/// <summary>
/// Helper for configuring a <see cref="ResourceBuilder"/> with
/// attributes that make traces and metrics easy to filter in observability tools.
/// </summary>
internal static class OtelResourceFactory
{
    /// <summary>
    /// Configures a <see cref="ResourceBuilder"/> with service-level attributes.
    /// </summary>
    public static void Configure(ResourceBuilder builder, OtelOptions options)
    {
        var serviceName = options.ServiceName
            ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name
            ?? "unknown-service";

        builder.AddService(
            serviceName: serviceName,
            serviceVersion: System.Reflection.Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString()
        );

        // ── Environment detection ─────────────────────────────────────
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                  ?? "Production";

        // ── Container ID (best-effort from cgroup) ────────────────────
        var containerId = DetectContainerId();

        var attributes = new Dictionary<string, object>
        {
            ["deployment.environment"] = env,
            ["host.name"] = Environment.MachineName,
            ["process.title"] = serviceName,
        };

        if (!string.IsNullOrEmpty(containerId))
            attributes["container.id"] = containerId;

        builder.AddAttributes(attributes);
    }

    private static string? DetectContainerId()
    {
        if (!File.Exists("/proc/self/cgroup"))
            return null;

        var lines = File.ReadAllLines("/proc/self/cgroup");

        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length >= 3)
            {
                var path = parts[^1].Trim();
                if (!string.IsNullOrEmpty(path) && path != "/")
                {
                    var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var candidate = segments.Last();

                    // Docker container IDs are 64-char hex strings
                    if (candidate.Length == 64 && IsHex(candidate))
                        return candidate;
                }
            }
        }

        return null;
    }

    private static bool IsHex(string s)
    {
        var hexDigits = "0123456789abcdefABCDEF";
        foreach (var c in s)
            if (!hexDigits.Contains(c))
                return false;
        return true;
    }
}
