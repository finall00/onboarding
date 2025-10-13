using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Worker.Infrastructure.Data;

namespace Worker.Infrastructure.RabbitMq;

public class RabbitMqPublisher
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly RabbitMqSettings _settings;
    private IConnection _conn;
    
    
    public RabbitMqPublisher(IConnectionFactory connectionFactory, ILogger<RabbitMqPublisher> logger,  RabbitMqSettings settings)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _settings = settings;
    }
    
    public  async Task PublishLeadListCreated(LeadListFailedMsg msg)
    {
        try
        {
            if (!IsConnected(_conn))
            {
                _conn = await _connectionFactory.CreateConnectionAsync();
            }

            await using var chan = await _conn.CreateChannelAsync();
            
            _logger.LogInformation("Declaring exchange '{ExchangeName}' as topic", _settings.Exchange);

            await chan.ExchangeDeclareAsync(
                exchange: _settings.Exchange,
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
            _logger.LogInformation("Publishing message to exchange '{ExchangeName}' with routing key '{RoutingKey}'", _settings.Exchange, _settings.RoutingKey);

            await chan.BasicPublishAsync(
                exchange: _settings.Exchange,
                routingKey: _settings.RoutingKey,
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