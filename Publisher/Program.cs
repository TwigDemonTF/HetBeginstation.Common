using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SharedOpenTelemetry;

/// <summary>
/// The main entry point for the inventory publisher service.
/// This service publishes messages to a RabbitMQ queue.
/// </summary>
var builder = Host.CreateApplicationBuilder(args);

/// <summary>
/// Configures OpenTelemetry for the publisher service with shared defaults.
/// </summary>
builder.AddSharedOpenTelemetry(options =>
{
    options.ServiceName = "inventory-publisher";
});

/// <summary>
/// Builds and starts the host for the application.
/// </summary>
using var host = builder.Build();
await host.StartAsync();

/// <summary>
/// Gets the logger for the publisher service.
/// </summary>
var logger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("Publisher");

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
/// Declares the queue that the publisher will send messages to.
/// </summary>
const string queue = "inventory.events";

await channel.QueueDeclareAsync(
    queue: queue,
    durable: true,
    exclusive: false,
    autoDelete: false);

/// <summary>
/// Creates a sample inventory event message to publish.
/// </summary>
var message = """
{
    "eventType": "inventory.updated",
    "productId": 123,
    "quantity": 42
}
""";

/// <summary>
/// Encodes the message to UTF-8 bytes for transmission.
/// </summary>
var body = Encoding.UTF8.GetBytes(message);

/// <summary>
/// Sets up message properties including content type and delivery mode.
/// </summary>
var properties = new BasicProperties
{
    ContentType = "application/json",
    DeliveryMode = DeliveryModes.Persistent
};

/// <summary>
/// Publishes the inventory event message to the queue.
/// </summary>
await channel.BasicPublishAsync(
    exchange: "",
    routingKey: queue,
    mandatory: false,
    basicProperties: properties,
    body: body);

/// <summary>
/// Logs that the event message was published successfully.
/// </summary>
logger.LogInformation("Published inventory.updated event");

/// <summary>
/// Stops the host gracefully.
/// </summary>
await host.StopAsync();