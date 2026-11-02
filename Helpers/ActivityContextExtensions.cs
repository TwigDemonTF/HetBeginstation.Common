namespace HetBeginstation.Common.Helpers;

/// <summary>
/// Helper methods for propagating trace context in scenarios not
/// covered by automatic instrumentation (e.g., background services,
/// custom message processing, background tasks).
/// </summary>
public static class ActivityContextExtensions
{
    /// <summary>
    /// Gets the current W3C traceparent string from the ambient
    /// <see cref="System.Diagnostics.Activity"/>.
    /// </summary>
    public static string? GetCurrentTraceparent()
    {
        return System.Diagnostics.Activity.Current?.Id;
    }

    /// <summary>
    /// Adds common attributes to the current activity for an event-based
    /// microservice message.
    /// </summary>
    public static void SetEventAttributes(
        string eventType,
        string correlationId,
        string aggregateType,
        string aggregateId,
        int retryCount = 0)
    {
        var activity = System.Diagnostics.Activity.Current;
        if (activity is null)
            return;

        activity.SetTag("event.type", eventType);
        activity.SetTag("event.correlation_id", correlationId);
        activity.SetTag("event.aggregate_type", aggregateType);
        activity.SetTag("event.aggregate_id", aggregateId);

        if (retryCount > 0)
            activity.SetTag("event.retry_count", retryCount);
    }

    /// <summary>
    /// Creates a child activity that wraps a message handler's execution.
    /// Use this for manual instrumentation of event handlers.
    /// </summary>
    public static System.Diagnostics.Activity? CreateHandlerActivity(
        string handlerName,
        object message)
    {
        var source = new System.Diagnostics.ActivitySource("HetBeginstation.EventHandlers");
        var activity = source.StartActivity(
            $"Handle.{handlerName}",
            System.Diagnostics.ActivityKind.Consumer
        );

        if (activity is not null)
        {
            var messageTypeName = message.GetType().Name;
            activity.SetTag("event.handler", handlerName);
            activity.SetTag("event.message_type", messageTypeName);
        }

        return activity;
    }

    /// <summary>
    /// Safely sets an error status on the current activity.
    /// </summary>
    public static void SetErrorStatus(Exception exception)
    {
        var activity = System.Diagnostics.Activity.Current;
        if (activity is null)
            return;

        activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error, exception.Message);
        activity.SetTag("exception.message", exception.Message);
        activity.SetTag("exception.stacktrace", exception.ToString());
        activity.SetTag("exception.type", exception.GetType().FullName);
    }
}
