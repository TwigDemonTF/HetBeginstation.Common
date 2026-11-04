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

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds consistent OpenTelemetry tracing, metrics, and logging defaults to an
    /// ASP.NET Core WebApplicationBuilder.
    /// </summary>
    public static HostApplicationBuilder AddSharedOpenTelemetry(
        this HostApplicationBuilder builder,
        Action<SharedOpenTelemetryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

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

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            throw new InvalidOperationException(
                "OpenTelemetry service name must not be empty.");
        }

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion);

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
                tracing.AddSource(RabbitMqTelemetry.ActivitySourceName);

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
