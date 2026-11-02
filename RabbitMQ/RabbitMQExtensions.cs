using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HetBeginstation.Common.RabbitMQ;

/// <summary>
/// Helper extensions for working with RabbitMQ.Client 7.x alongside OpenTelemetry.
/// <para>
/// RabbitMQ.Client 7.x has built-in OpenTelemetry support via
/// <c>RabbitMQActivitySource</c> that creates spans for publish/consume
/// operations and handles W3C trace context propagation automatically.
/// </para>
/// <para>
/// These extensions add business-domain metadata on top of that by overriding
/// the built-in consumer's message handler.
/// </para>
/// </summary>
public static class RabbitMQExtensions
{
    /// <summary>
    /// Creates an <see cref="AsyncEventingBasicConsumer"/> that enriches every
    /// received-message span with business-domain attributes before invoking
    /// the handler delegate.
    /// </summary>
    public static AsyncEventingBasicConsumer CreateTrackedConsumer(
        IChannel channel,
        Func<string, ulong, bool, string, string, IReadOnlyBasicProperties, ReadOnlyMemory<byte>, CancellationToken, Task> handler)
    {
        return new TrackedAsyncConsumer(channel, handler);
    }

    /// <summary>
    /// Convenience method to publish a message with trace context
    /// and semantic attributes automatically set.
    /// </summary>
    public static async Task PublishAsync(
        this IChannel channel,
        string exchange,
        string routingKey,
        ReadOnlyMemory<byte> body,
        string? eventType = null,
        string? correlationId = null,
        string? messageId = null,
        string? aggregateType = null,
        string? aggregateId = null,
        CancellationToken cancellationToken = default)
    {
        var props = new BasicProperties
        {
            Headers = new Dictionary<string, object?>()
        };

        if (!string.IsNullOrEmpty(eventType))
            props.Headers!["event_type"] = eventType;
        if (!string.IsNullOrEmpty(correlationId))
            props.Headers!["correlation_id"] = correlationId;
        if (!string.IsNullOrEmpty(messageId))
            props.Headers!["message_id"] = messageId;
        if (!string.IsNullOrEmpty(aggregateType))
            props.Headers!["aggregate_type"] = aggregateType;
        if (!string.IsNullOrEmpty(aggregateId))
            props.Headers!["aggregate_id"] = aggregateId;

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);

        // Enrich the current activity span
        var activity = System.Diagnostics.Activity.Current;
        if (activity is not null)
        {
            activity.SetTag(RabbitMQEventSemanticConventions.AttributeExchange, exchange);
            activity.SetTag(RabbitMQEventSemanticConventions.AttributeRoutingKey, routingKey);
            if (!string.IsNullOrEmpty(eventType))
                activity.SetTag(RabbitMQEventSemanticConventions.AttributeEventType, eventType);
        }
    }

    /// <summary>
    /// Injects W3C trace context into a properties builder.
    /// Use this when constructing publish properties manually.
    /// </summary>
    public static void InjectTraceContext(this BasicProperties properties)
    {
        var activity = System.Diagnostics.Activity.Current;
        if (activity is null)
            return;

        var traceparent = activity.Id;
        if (string.IsNullOrEmpty(traceparent))
            return;

        var headers = properties.Headers ?? new Dictionary<string, object?>();
        headers["traceparent"] = traceparent;
        properties.Headers = headers;
    }

    /// <summary>
    /// Extracts the traceparent from message headers.
    /// </summary>
    public static string? ExtractTraceparent(IReadOnlyBasicProperties properties)
    {
        var headers = properties.Headers as IDictionary<string, object?>;
        if (headers?.TryGetValue("traceparent", out var value) is true)
            return value as string;

        return null;
    }

    /// <summary>
    /// Internal consumer that enriches spans with business-domain attributes.
    /// </summary>
    private sealed class TrackedAsyncConsumer : AsyncEventingBasicConsumer
    {
        private readonly Func<string, ulong, bool, string, string, IReadOnlyBasicProperties, ReadOnlyMemory<byte>, CancellationToken, Task> _handler;

        public TrackedAsyncConsumer(IChannel channel, Func<string, ulong, bool, string, string, IReadOnlyBasicProperties, ReadOnlyMemory<byte>, CancellationToken, Task> handler)
            : base(channel)
        {
            _handler = handler;
        }

        public override async Task HandleBasicDeliverAsync(
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IReadOnlyBasicProperties properties,
            ReadOnlyMemory<byte> body,
            CancellationToken cancellationToken)
        {
            // Enrich the current RabbitMQ span with semantic attributes
            var activity = System.Diagnostics.Activity.Current;
            if (activity is not null)
            {
                if (!string.IsNullOrEmpty(routingKey))
                    activity.SetTag(RabbitMQEventSemanticConventions.AttributeRoutingKey, routingKey);

                if (!string.IsNullOrEmpty(exchange))
                    activity.SetTag(RabbitMQEventSemanticConventions.AttributeExchange, exchange);

                if (redelivered)
                    activity.SetTag(RabbitMQEventSemanticConventions.AttributeRetryCount, 1);

                var headers = properties.Headers as IDictionary<string, object?>;
                if (headers?.TryGetValue("message_id", out var messageId) is true)
                    activity.SetTag(RabbitMQEventSemanticConventions.AttributeMessageId, messageId?.ToString());

                if (headers?.TryGetValue("correlation_id", out var correlationId) is true)
                    activity.SetTag(RabbitMQEventSemanticConventions.AttributeCorrelationId, correlationId?.ToString());

                if (headers?.TryGetValue("event_type", out var eventType) is true)
                    activity.SetTag(RabbitMQEventSemanticConventions.AttributeEventType, eventType?.ToString());
            }

            try
            {
                await _handler(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body, cancellationToken);
            }
            catch (Exception ex)
            {
                Helpers.ActivityContextExtensions.SetErrorStatus(ex);
                throw;
            }
        }
    }
}
