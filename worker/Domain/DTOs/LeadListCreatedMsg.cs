namespace Worker.Domain.DTOs;

public class LeadListCreatedMsg
{
    public Guid LeadListId { get; set; }
    public Guid CorrelationId { get; set; }
    public string SourceUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}