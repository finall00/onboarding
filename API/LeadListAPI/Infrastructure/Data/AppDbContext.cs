using leadListAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace leadListAPI.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<LeadList> LeadLists { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeadList>(entity =>
        {
            entity.ToTable("lead_lists");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SourceUrl).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}