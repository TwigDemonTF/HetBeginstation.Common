using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace PublisherAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly ConnectionFactory _factory;

        public UsersController(ILogger<UsersController> logger)
        {
            _logger = logger;
            _factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") 
                    ?? "localhost"
            };
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // In a real application, this would involve database operations
                // For this example, we'll just simulate creating a user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Email = request.Email
                };

                // Create the event to publish
                var userCreatedEvent = new UserCreatedEvent
                {
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Timestamp = DateTime.UtcNow
                };

                // Publish the event to RabbitMQ
                await PublishUserCreatedEvent(userCreatedEvent);

                _logger.LogInformation("User created and event published: {UserId}", user.Id);

                return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task PublishUserCreatedEvent(UserCreatedEvent userEvent)
        {
            await using var connection = await _factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            const string queue = "user.events";
            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            var message = JsonSerializer.Serialize(userEvent);
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

            _logger.LogInformation("Published user created event for user {UserId}", userEvent.UserId);
        }
    }

    public class CreateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UserCreatedEvent
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}