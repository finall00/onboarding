namespace Worker.Infrastructure.RabbitMq;

public class RabbitMqSettings
{
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string? User { get; set; }
    public string? Pass { get; set; }
    public string Exchange { get; set; } = "leadlists";
    public string QueueName { get; set; } = "leadlists.worker"; 
    public string RoutingKey { get; set; } = "leadlists.failed";
    public ushort PrefetchCount { get; set; } = 1;
}