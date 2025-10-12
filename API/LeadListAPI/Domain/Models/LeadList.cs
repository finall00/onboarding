using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace leadListAPI.Domain.Models;
    
[Table("lead_lists")]
public class LeadList
{
    
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("source_url")]
    public string SourceUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public LeadListStatus Status { get; set; } = LeadListStatus.Pending;

    [Required]
    [Column("processed_count")]
    public int ProcessedCount { get; set; } = 0;

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("correlation_id")]
    public Guid CorrelationId { get; set; } = Guid.NewGuid();


    public bool IsEditable()
    {
        return this.Status == LeadListStatus.Failed || this.Status == LeadListStatus.Pending;
    }
    public bool IsDeletable()
    {
        return this.Status == LeadListStatus.Failed || this.Status == LeadListStatus.Pending;
    }
}

