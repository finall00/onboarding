namespace API.Domain.DTOs;

public class LeadListCreatedMsg
{
    public Guid LeadListId { get; set; }
    public Guid CorrelationId { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}