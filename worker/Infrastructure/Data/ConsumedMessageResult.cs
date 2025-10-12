using Worker.Domain.DTOs;

namespace Worker.Infrastructure.Data;

public class ConsumedMessageResult
{
    public bool Found { get; init; }
    public LeadListCreatedMsg? Message { get; init; }
}