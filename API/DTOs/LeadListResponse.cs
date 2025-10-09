namespace API.DTOs;

public class LeadListResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CorrelationId { get; set; }
}