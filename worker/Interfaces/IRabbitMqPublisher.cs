using Worker.Domain.Models;

namespace Worker.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishLeadList(LeadList leadList);
}