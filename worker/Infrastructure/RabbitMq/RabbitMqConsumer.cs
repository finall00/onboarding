using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Worker.Domain.DTOs;
using Worker.Infrastructure.Data;

namespace Worker.Infrastructure.RabbitMq;

public class RabbitMqConsumer : IRabbitMqConsumer
{
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly RabbitMqSettings _settings;
    private readonly IConnectionFactory _factory;

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public RabbitMqConsumer(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqConsumer> logger,
        IConnectionFactory factory)
    {
        _settings = settings.Value;
        _logger = logger;
        _factory = factory;
    }

    public bool IsConnected => _connection?.IsOpen == true && _channel?.IsOpen == true;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("RabbitMqConsumer connecting...");

            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: _settings.PrefetchCount,
                global: false,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Connected to RabbitMQ");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error connecting RabbitMQ");
            throw;
        }
    }

    public async Task<ConsumedMessageResult> ConsumeMessageAsync(Guid correlationId, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Cannot consume message, RabbitMQ is not connected.");
        }

        var stop = Stopwatch.StartNew();
        _logger.LogInformation("Searching for message with CorrelationId {CorrelationId} in queue '{QueueName}'...",
            correlationId, _settings.QueueName);

        while (stop.Elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            var result = await _channel?.BasicGetAsync(_settings.QueueName, autoAck: false,
                cancellationToken: cancellationToken);

            if (result != null)
            {
                var msg = DeserializeMessage(result.Body.ToArray());

                if (msg?.CorrelationId == correlationId)
                {
                    _logger.LogInformation("Found matching message with DeliveryTag {DeliveryTag}.",
                        result.DeliveryTag);
                    await AckMsgAsync(result.DeliveryTag);
                    return new ConsumedMessageResult { Found = true, Message = msg };
                }

                _logger.LogWarning("Found message for another job (CorrelationId: {MsgCorrelationId}). Requeuing it.",
                    msg?.CorrelationId);
                await NackMsgAsync(result.DeliveryTag, requeue: true);
            }

            await Task.Delay(1000, cancellationToken);
        }

        _logger.LogWarning("Timeout reached. Message with CorrelationId {CorrelationId} not found.", correlationId);
        return new ConsumedMessageResult { Found = false };
    }

    public ValueTask AckMsgAsync(ulong deliveryTag)
    {
        if (IsConnected)
            return _channel!.BasicAckAsync(deliveryTag, multiple: false);

        _logger.LogError("Cannot ACK message, RabbitMqConsumer is not connected");
        return ValueTask.CompletedTask;
    }

    public ValueTask NackMsgAsync(ulong deliveryTag, bool requeue)
    {
        if (IsConnected)
            return _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: requeue);

        _logger.LogWarning("Cannot NACK message, RabbitMQConsumer is not connected.");
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogInformation("Disposing RabbitMQ resources asynchronously...");

        try
        {
            // Agora usamos 'await' corretamente em um método 'async'.
            if (_channel is { IsOpen: true }) await _channel.CloseAsync();
            if (_connection is { IsOpen: true }) await _connection.CloseAsync();

            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An error occurred during RabbitMQ resource disposal.");
        }
    }
    
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private LeadListCreatedMsg? DeserializeMessage(byte[] body)
    {
        try
        {
            var msgJson = Encoding.UTF8.GetString(body);
            return JsonSerializer.Deserialize<LeadListCreatedMsg>(msgJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message. It might be a poison message.");
            return null;
        }
    }
}