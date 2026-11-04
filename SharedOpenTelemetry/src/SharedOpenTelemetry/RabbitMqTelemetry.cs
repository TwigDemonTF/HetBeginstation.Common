using System.Diagnostics;

namespace SharedOpenTelemetry;

public static class RabbitMqTelemetry
{
    public const string ActivitySourceName = "SharedOpenTelemetry.RabbitMQ";

    private static readonly ActivitySource Source =
        new(ActivitySourceName);

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