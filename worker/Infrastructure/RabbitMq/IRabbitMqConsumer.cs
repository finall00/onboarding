using Worker.Domain.DTOs;

namespace Worker.Infrastructure.RabbitMq;

public interface IRabbitMqConsumer : IDisposable
{

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task StartConsumingAsync(Func<LeadListCreatedMsg, ulong, Task<bool>> onMessageReceivedAsync,
        CancellationToken cancellationToken = default);
    
    ValueTask AckMsgAsync(ulong deliveryTag);
    
    ValueTask NackMsgAsync(ulong deliveryTag, bool requeue);
    
    bool IsConnected { get; }
}