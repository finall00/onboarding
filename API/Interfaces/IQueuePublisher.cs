using API.Domain.DTOs;

namespace API.Interfaces;

public interface IQueuePublisher
{
    Task PublishLeadListCreated(LeadListCreatedMsg msg);
}