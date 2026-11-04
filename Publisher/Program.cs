using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SharedOpenTelemetry;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSharedOpenTelemetry(options =>
{
    options.ServiceName = "inventory-publisher";
});

using var host = builder.Build();
await host.StartAsync();

var logger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("Publisher");

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

var message = """
{
    "eventType": "inventory.updated",
    "productId": 123,
    "quantity": 42
}
""";

var body = Encoding.UTF8.GetBytes(message);

var properties = new BasicProperties
{
    ContentType = "application/json",
    DeliveryMode = DeliveryModes.Persistent
};

await channel.BasicPublishAsync(
    exchange: "",
    routingKey: queue,
    mandatory: false,
    basicProperties: properties,
    body: body);

logger.LogInformation("Published inventory.updated event");

await host.StopAsync();