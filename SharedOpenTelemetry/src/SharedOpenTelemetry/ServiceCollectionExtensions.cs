using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SharedOpenTelemetry;

/// <summary>
/// Provides extension methods for configuring OpenTelemetry services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds consistent OpenTelemetry tracing, metrics, and logging defaults to an
    /// ASP.NET Core WebApplicationBuilder.
    /// </summary>
    /// <param name="builder">The host builder to configure.</param>
    /// <param name="configure">Optional configuration action to customize the OpenTelemetry options.</param>
    /// <returns>The configured host builder.</returns>
    public static HostApplicationBuilder AddSharedOpenTelemetry(
        this HostApplicationBuilder builder,
        Action<SharedOpenTelemetryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        /// <summary>
        /// Initializes OpenTelemetry options with default values from configuration or environment.
        /// </summary>
        var options = new SharedOpenTelemetryOptions
        {
            ServiceName =
                builder.Configuration["OpenTelemetry:ServiceName"]
                ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
                ?? builder.Environment.ApplicationName,
            ServiceVersion = builder.Configuration["OpenTelemetry:ServiceVersion"],
            EnableOtlpExporter = builder.Configuration.GetValue<bool?>(
                "OpenTelemetry:EnableOtlpExporter") ?? true
        };

        configure?.Invoke(options);

        /// <summary>
        /// Validates that the service name is not empty.
        /// </summary>
        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            throw new InvalidOperationException(
                "OpenTelemetry service name must not be empty.");
        }

        /// <summary>
        /// Creates a resource builder with service information.
        /// </summary>
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion);

        /// <summary>
        /// Configures OpenTelemetry logging with structured logging support.
        /// </summary>
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;

            if (options.EnableOtlpExporter)
            {
                logging.AddOtlpExporter();
            }
        });

        /// <summary>
        /// Configures OpenTelemetry tracing, metrics, and logs with standard instrumentation.
        /// </summary>
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: options.ServiceName,
                    serviceVersion: options.ServiceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRabbitMQInstrumentation();

                foreach (var sourceName in options.ActivitySourceNames
                             .Where(name => !string.IsNullOrWhiteSpace(name)))
                {
                    tracing.AddSource(sourceName);
                }
                
                // Add the RabbitMQ and OpenTelemetryExporter sources
                tracing.AddSource(RabbitMqTelemetry.ActivitySourceName);
                tracing.AddOtlpExporter();

                if (options.EnableOtlpExporter)
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (options.EnableOtlpExporter)
                {
                    metrics.AddOtlpExporter();
                }
            });

        return builder;
    }
}
