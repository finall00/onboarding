
using Worker.Infrastructure.Data;

namespace Worker.Infrastructure.RabbitMq;

public interface IRabbitMqPublisher
{
    Task PublishLeadListCreated(LeadListFailedMsg msg);
}