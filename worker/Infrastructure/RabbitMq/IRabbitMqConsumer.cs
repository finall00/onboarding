using Worker.Infrastructure.Data;

namespace Worker.Infrastructure.RabbitMq;

public interface IRabbitMqConsumer : IDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task<ConsumedMessageResult> ConsumeMessageAsync(Guid correlationId, TimeSpan timeout, CancellationToken cancellationToken);
    ValueTask AckMsgAsync(ulong deliveryTag);
    ValueTask NackMsgAsync(ulong deliveryTag, bool requeue);
    ValueTask DisposeAsync();
}