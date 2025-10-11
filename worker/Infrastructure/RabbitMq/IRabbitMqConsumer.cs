using Worker.Domain.DTOs;

namespace Worker.Infrastructure.RabbitMq;

public interface IRabbitMqConsumer : IDisposable
{

    Task Connect(CancellationToken cancellationToken = default);

    Task StartConsuming(Func<LeadListCreatedMsg, ulong, Task<bool>> onMessageReceivedAsync,
        CancellationToken cancellationToken = default);
    
    ValueTask AckMsg(ulong deliveryTag, bool requeue);
    
    ValueTask NackMsg(ulong deliveryTag, bool requeue);
    
    bool IsConnected { get; }

}