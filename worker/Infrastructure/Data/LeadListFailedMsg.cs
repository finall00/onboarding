namespace Worker.Infrastructure.Data;

public class LeadListFailedMsg(Guid LeadListId, Guid CorrelationId, DateTime CreatedAt, string SourceUrl)
{
    public Guid LeadListId { get; set; } = LeadListId;
    public Guid CorrelationId { get; set; } = CorrelationId;
    public string SourceUrl { get; set; } = SourceUrl;
    public DateTime CreatedAt { get; set; } = CreatedAt;
}