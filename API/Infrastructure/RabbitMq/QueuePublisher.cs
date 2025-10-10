using System.Text;
using System.Text.Json;
using API.Domain.DTOs;
using API.Interfaces;
using RabbitMQ.Client;

namespace API.Infrastructure.RabbitMq;

public class QueuePublisher : IQueuePublisher
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<QueuePublisher> _logger;
    private IConnection _conn;
    
    private const string ExchangeName = "leadlists";
    private const string RoutingKey = "leadlist.created";
    
    public QueuePublisher(IConnectionFactory connectionFactory, ILogger<QueuePublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }
    
    public  async Task PublishLeadListCreated(LeadListCreatedMsg msg)
    {
        try
        {
            if (!IsConnected(_conn))
            {
                _conn = await _connectionFactory.CreateConnectionAsync();
            }

            await using var chan = await _conn.CreateChannelAsync();
            
            _logger.LogInformation("Declaring exchange '{ExchangeName}' as topic", ExchangeName);

            await chan.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete:false
                );

            var msgBody = JsonSerializer.Serialize(msg);
            var bodyBytes = Encoding.UTF8.GetBytes(msgBody);

            var properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent
            };
            _logger.LogInformation("Publishing message to exchange '{ExchangeName}' with routing key '{RoutingKey}'", ExchangeName, RoutingKey);

            await chan.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: bodyBytes
            );
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to Queue. msg: {msg}", msg);
            throw;
        }
    }

    private static bool IsConnected(IConnection? connection)
    {
        return connection is { IsOpen: true };
    }
    
    
}