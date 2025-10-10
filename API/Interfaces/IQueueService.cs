using API.DTOs;

namespace API.Interfaces;

public interface IQueueService
{
    Task PublishLeadListCreated(LeadListCreatedMsg msg);
}