using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedOpenTelemetry;

/// <summary>
/// The main entry point for the inventory consumer service.
/// This service consumes messages from a RabbitMQ queue and processes them.
/// </summary>
var builder = Host.CreateApplicationBuilder(args);

/// <summary>
/// Configures OpenTelemetry for the consumer service with shared defaults.
/// </summary>
builder.AddSharedOpenTelemetry(options =>
{
    options.ServiceName = "inventory-consumer";
});

/// <summary>
/// Builds and starts the host for the application.
/// </summary>
using var host = builder.Build();
await host.StartAsync();

/// <summary>
/// Gets the logger for the consumer service.
/// </summary>
var logger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("Consumer");

/// <summary>
/// Creates a RabbitMQ connection factory with configurable host.
/// </summary>
var factory = new ConnectionFactory
{
    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
        ?? "localhost"
};

/// <summary>
/// Establishes a connection to RabbitMQ and creates a channel for message operations.
/// </summary>
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

/// <summary>
/// Declares the queue that the consumer will listen to for inventory events.
/// </summary>
const string queue = "inventory.events";

await channel.QueueDeclareAsync(
    queue: queue,
    durable: true,
    exclusive: false,
    autoDelete: false);

/// <summary>
/// Creates an async consumer that processes incoming messages.
/// </summary>
var consumer = new AsyncEventingBasicConsumer(channel);

/// <summary>
/// Handles incoming messages from the queue.
/// </summary>
consumer.ReceivedAsync += async (_, ea) =>
{
    try
    {
        /// <summary>
        /// Decodes the message body from UTF-8 encoding.
        /// </summary>
        var message = Encoding.UTF8.GetString(ea.Body.ToArray());

        /// <summary>
        /// Logs the received inventory event message.
        /// </summary>
        logger.LogInformation(
            "Received inventory event: {Message}",
            message);

        // Process the event here.

        /// <summary>
        /// Acknowledges successful processing of the message.
        /// </summary>
        await channel.BasicAckAsync(
            deliveryTag: ea.DeliveryTag,
            multiple: false);
    }
    catch (Exception ex)
    {
        /// <summary>
        /// Logs an error when processing fails and requeues the message.
        /// </summary>
        logger.LogError(ex, "Failed to process inventory event");

        // Requeue this example's failed message.
        await channel.BasicNackAsync(
            deliveryTag: ea.DeliveryTag,
            multiple: false,
            requeue: true);
    }
};

/// <summary>
/// Starts consuming messages from the queue.
/// </summary>
await channel.BasicConsumeAsync(
    queue: queue,
    autoAck: false,
    consumer: consumer);

/// <summary>
/// Logs that the service is waiting for inventory events.
/// </summary>
logger.LogInformation("Waiting for inventory events...");

/// <summary>
/// Waits for shutdown signal to gracefully terminate the service.
/// </summary>
await host.WaitForShutdownAsync();