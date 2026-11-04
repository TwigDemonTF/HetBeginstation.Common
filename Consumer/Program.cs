using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedOpenTelemetry;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSharedOpenTelemetry(options =>
{
    options.ServiceName = "inventory-consumer";
});

using var host = builder.Build();
await host.StartAsync();

var logger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("Consumer");

var factory = new ConnectionFactory
{
    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
        ?? "localhost"
};

await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

const string queue = "inventory.events";

await channel.QueueDeclareAsync(
    queue: queue,
    durable: true,
    exclusive: false,
    autoDelete: false);

var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (_, ea) =>
{
    try
    {
        var message = Encoding.UTF8.GetString(ea.Body.ToArray());

        logger.LogInformation(
            "Received inventory event: {Message}",
            message);

        // Process the event here.

        await channel.BasicAckAsync(
            deliveryTag: ea.DeliveryTag,
            multiple: false);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to process inventory event");

        // Requeue this example's failed message.
        await channel.BasicNackAsync(
            deliveryTag: ea.DeliveryTag,
            multiple: false,
            requeue: true);
    }
};

await channel.BasicConsumeAsync(
    queue: queue,
    autoAck: false,
    consumer: consumer);

logger.LogInformation("Waiting for inventory events...");

await host.WaitForShutdownAsync();