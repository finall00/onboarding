using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Worker.Domain.DTOs;

namespace Worker.Infrastructure.RabbitMq;

public class RabbitMqConsumer : IRabbitMqConsumer
{

    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly RabbitMqSettings _settings;
    private readonly IConnectionFactory _factory;
    
    
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public RabbitMqConsumer(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqConsumer> logger, IConnectionFactory factory)
    {
        _settings = settings.Value;
        _logger = logger;
        _factory = factory;
    }
    
    public  async Task Connect(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("RabbitMqConsumer connecting...");

            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: _settings.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);
            
            await _channel.QueueDeclareAsync(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);
            
            _logger.LogInformation("Declared Queue {QueueName}", _settings.QueueName);
            
            await _channel.QueueBindAsync(
                queue: _settings.QueueName,
                exchange: _settings.Exchange,
                routingKey: _settings.RoutingKey,
                arguments: null,
                cancellationToken: cancellationToken);
            
            _logger.LogInformation("Binding  Queue '{QueueName}' to '{Exchange}' ", _settings.QueueName, _settings.Exchange);

            await _channel.BasicQosAsync(
            prefetchSize:0,
            prefetchCount: _settings.PrefetchCount,
            global: false,
            cancellationToken: cancellationToken
                );
            _logger.LogInformation("Connected to RabbitMQ");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error connecting RabbitMQ");
        }
    }

    public async Task StartConsuming(Func<LeadListCreatedMsg, ulong, Task<bool>> onMessageReceived, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.LogError("RabbitMq cant Connect");
            throw new InvalidOperationException("RabbitMq cant Connect");
        }

        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            
            var body  = eventArgs.Body.ToArray();
            var msgJson = Encoding.UTF8.GetString(body);
            var deliveryTag = eventArgs.DeliveryTag;
            
            _logger.LogInformation("Received message {Message} from queue {QueueName}", _settings.QueueName, _settings.Exchange);

            try
            {
                var opt = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var msg = JsonSerializer.Deserialize<LeadListCreatedMsg>(msgJson, opt);

                if (msg == null)
                {
                    _logger.LogWarning("Invalid message received from queue. Permanently reject");
                    await NackMsg(deliveryTag, false);
                    return;
                }

                var ack = await onMessageReceived(msg, deliveryTag);

                if (!ack)
                {
                    _logger.LogWarning("callback return false. message will not be processed");
                }

            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserialize message from {QueueName}. reject permanently",
                    _settings.QueueName);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected exception");
                await NackMsg(deliveryTag, false);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: _settings.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
        _logger.LogInformation("Consumer registered");
    }

    public ValueTask AckMsg(ulong deliveryTag, bool requeue)
    {
        if (IsConnected)
        {
            return _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: requeue);
        }
        
        _logger.LogError("RabbitMqConsumer is not connected");
        return ValueTask.CompletedTask;
    }

    public ValueTask NackMsg(ulong deliveryTag, bool requeue)
    {
        if (IsConnected)
        {
            return _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: requeue);
        }
        
        _logger.LogError("RabbitMqConsumer is not connected");
        return ValueTask.CompletedTask;
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _logger.LogInformation("Cleaning RabbitMq resources");
        
        try
        {
            if (_channel?.IsOpen == true)
            {
                _channel.CloseAsync().GetAwaiter().GetResult();
                _logger.LogInformation("channel closed");
            }

            if (_connection?.IsOpen == true)
            {
                _connection.CloseAsync().GetAwaiter().GetResult();
                _logger.LogInformation("connection closed");
            }

            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing RabbitMq");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public bool IsConnected => _connection?.IsOpen == true &&  _channel?.IsOpen == true;
}