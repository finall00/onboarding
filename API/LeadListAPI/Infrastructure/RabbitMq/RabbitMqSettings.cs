namespace leadListAPI.Infrastructure.RabbitMq;

public class RabbitMqSettings
{
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string? User { get; set; } 
    public string? Pass { get; set; }
    public string? Exchange { get; set; }
    public string? QueueName { get; set; }
    public string? RoutingKey { get; set; }
    public ushort PrefetchCount { get; set; } = 1;
}