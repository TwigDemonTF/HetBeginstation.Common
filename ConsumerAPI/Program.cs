using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedOpenTelemetry;
using System.Text;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

// Configure OpenTelemetry
builder.AddSharedOpenTelemetry(options =>
{
    options.ServiceName = "user-consumer-service";
});

// Build and start the host
using var host = builder.Build();
await host.StartAsync();

// Get logger
var logger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("UserConsumerService");

// Create RabbitMQ connection factory
var factory = new ConnectionFactory
{
    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
        ?? "localhost"
};

// Establish connection to RabbitMQ and create channel
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

// Declare the queue that the consumer will listen to
const string queue = "user.events";
await channel.QueueDeclareAsync(
    queue: queue,
    durable: true,
    exclusive: false,
    autoDelete: false);

// Create async consumer that processes incoming messages
var consumer = new AsyncEventingBasicConsumer(channel);

// Handle incoming messages from the queue
consumer.ReceivedAsync += async (_, ea) =>
{
    try
    {
        // Decode the message body from UTF-8 encoding
        var message = Encoding.UTF8.GetString(ea.Body.ToArray());

        // Deserialize the JSON message
        var userEvent = JsonSerializer.Deserialize<UserCreatedEvent>(message);

        // Log the received user event message
        logger.LogInformation(
            "Received user created event: UserId={UserId}, Name={Name}, Email={Email}",
            userEvent?.UserId, userEvent?.Name, userEvent?.Email);

        // Process the user creation event (in this example, just log it)
        // In a real scenario, this might involve:
        // - Saving to a database
        // - Sending notifications
        // - Updating related systems

        // Simulate some processing work
        await Task.Delay(100);

        // Acknowledge successful processing of the message
        await channel.BasicAckAsync(
            deliveryTag: ea.DeliveryTag,
            multiple: false);

        logger.LogInformation("Successfully processed user created event for user {UserId}", userEvent?.UserId);
    }
    catch (Exception ex)
    {
        // Log error when processing fails and requeue the message
        logger.LogError(ex, "Failed to process user created event");

        // Requeue this failed message
        await channel.BasicNackAsync(
            deliveryTag: ea.DeliveryTag,
            multiple: false,
            requeue: true);
    }
};

// Start consuming messages from the queue
await channel.BasicConsumeAsync(
    queue: queue,
    autoAck: false,
    consumer: consumer);

logger.LogInformation("Waiting for user created events...");

// Wait for shutdown signal to gracefully terminate the service
await host.WaitForShutdownAsync();