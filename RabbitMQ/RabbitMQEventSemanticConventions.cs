namespace HetBeginstation.Common.RabbitMQ;

/// <summary>
/// Semantic attribute names for event/messaging spans that complement the
/// official OpenTelemetry semantic conventions.
/// </summary>
public static class RabbitMQEventSemanticConventions
{
    /// <summary>
    /// The event type or command name, e.g., "OrderCreated", "UserUpdated".
    /// Use this to tag spans for business-domain correlation.
    /// </summary>
    public const string AttributeEventType = "event.type";

    /// <summary>
    /// A stable correlation ID for the business operation.
    /// </summary>
    public const string AttributeCorrelationId = "event.correlation_id";

    /// <summary>
    /// The aggregate ID the event pertains to, e.g., an Order ID.
    /// </summary>
    public const string AttributeAggregateId = "event.aggregate_id";

    /// <summary>
    /// The aggregate type, e.g., "Order", "User".
    /// </summary>
    public const string AttributeAggregateType = "event.aggregate_type";

    /// <summary>
    /// The message ID from the event envelope.
    /// </summary>
    public const string AttributeMessageId = "event.message_id";

    /// <summary>
    /// The retry attempt number for redelivered messages.
    /// </summary>
    public const string AttributeRetryCount = "event.retry_count";

    /// <summary>
    /// The exchange the message was published to.
    /// </summary>
    public const string AttributeExchange = "rabbitmq.exchange";

    /// <summary>
    /// The routing key used for the message.
    /// </summary>
    public const string AttributeRoutingKey = "rabbitmq.routing_key";
}
