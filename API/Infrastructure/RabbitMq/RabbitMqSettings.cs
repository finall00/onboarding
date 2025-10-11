namespace API.Infrastructure.RabbitMq;

public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "leadlists";
    public string QueueName { get; set; } = "leadlists.worker";
    public string RoutingKey { get; set; } = "leadlist.created";
    public ushort PrefetchCount { get; set; } = 1;
}