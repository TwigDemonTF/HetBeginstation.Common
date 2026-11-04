using System.Diagnostics;

namespace SharedOpenTelemetry;

/// <summary>
/// Provides helper methods for creating RabbitMQ telemetry activities.
/// This class is used to instrument RabbitMQ message publishing and processing.
/// </summary>
public static class RabbitMqTelemetry
{
    /// <summary>
    /// Gets the name of the ActivitySource used for RabbitMQ telemetry.
    /// </summary>
    public const string ActivitySourceName = "SharedOpenTelemetry.RabbitMQ";

    /// <summary>
    /// Gets the static ActivitySource instance for RabbitMQ telemetry.
    /// </summary>
    private static readonly ActivitySource Source =
        new(ActivitySourceName);

    /// <summary>
    /// Starts a new Activity for publishing a message to RabbitMQ.
    /// </summary>
    /// <param name="eventName">The name of the event being published.</param>
    /// <returns>The started Activity or null if no activity was started.</returns>
    public static Activity? StartPublish(string eventName)
    {
        var activity = Source.StartActivity(
            $"publish {eventName}",
            ActivityKind.Producer);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.name", "publish");
        activity?.SetTag("messaging.destination.name", eventName);

        return activity;
    }

    /// <summary>
    /// Starts a new Activity for processing a message from RabbitMQ.
    /// </summary>
    /// <param name="eventName">The name of the event being processed.</param>
    /// <returns>The started Activity or null if no activity was started.</returns>
    public static Activity? StartProcess(string eventName)
    {
        var activity = Source.StartActivity(
            $"process {eventName}",
            ActivityKind.Consumer);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.name", "process");
        activity?.SetTag("messaging.destination.name", eventName);

        return activity;
    }
}