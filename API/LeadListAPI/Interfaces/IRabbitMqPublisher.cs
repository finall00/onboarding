using leadListAPI.Domain.DTOs;

namespace leadListAPI.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishLeadListCreated(LeadListCreatedMsg msg);
}