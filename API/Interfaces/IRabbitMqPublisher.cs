using API.Domain.DTOs;

namespace API.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishLeadListCreated(LeadListCreatedMsg msg);
}