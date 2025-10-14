using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Worker.Domain.Models;
using Worker.Infrastructure.Data;
using Worker.Interfaces;

namespace Worker.Infrastructure.RabbitMq;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly RabbitMqSettings _settings;
    private IConnection _conn;
    
    
    public RabbitMqPublisher(IConnectionFactory connectionFactory, ILogger<RabbitMqPublisher> logger, IOptions<RabbitMqSettings> settings)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _settings = settings.Value;
    }
    
    public  async Task PublishLeadList(LeadList leadList)
    {
        try
        {
            if (!IsConnected(_conn))
            {
                _conn = await _connectionFactory.CreateConnectionAsync();
            }

            await using var chan = await _conn.CreateChannelAsync();

            _logger.LogInformation("Declaring exchange '{Exchange}'", _settings.Exchange);
            await chan.ExchangeDeclareAsync(
                exchange: _settings.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            _logger.LogInformation("Declaring queue '{Queue}'", _settings.QueueName);
            await chan.QueueDeclareAsync(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            _logger.LogInformation("Binding queue '{Queue}' to exchange '{Exchange}' with routing key '{RoutingKey}'",
                _settings.QueueName, _settings.Exchange, _settings.RoutingKey);
            await chan.QueueBindAsync(
                queue: _settings.QueueName,
                exchange: _settings.Exchange,
                routingKey: _settings.RoutingKey
            );
           
            var msg = new LeadListFailedMsg(leadList.Id, leadList.CorrelationId, leadList.CreatedAt, leadList.SourceUrl);
            
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
                body: bodyBytes
            );
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to Queue.");
            throw;
        }
    }

    private static bool IsConnected(IConnection? connection)
    {
        return connection is { IsOpen: true };
    }
}
