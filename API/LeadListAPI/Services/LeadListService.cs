using leadListAPI.Domain.DTOs;
using leadListAPI.Domain.Models;
using leadListAPI.Infrastructure.Data;
using leadListAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace leadListAPI.Services;

public class LeadListService : ILeadListService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LeadListService> _logger;
    private readonly IJobCreator _jobCreator;

    public LeadListService(
        AppDbContext context, 
        ILogger<LeadListService> logger, 
        IJobCreator jobCreator)
    {
        _context = context;
        _logger = logger;
        _jobCreator = jobCreator;
    }

    public async Task<PagedResult<LeadListResponse>> GetAll(int page, int pageSize, string? status, string? q)
    {
        var query = _context.LeadLists.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeadListStatus>(status, true, out var statusEnum))
            query = query.Where(l => l.Status == statusEnum);

        if (!string.IsNullOrEmpty(q))
            query = query.Where(l => l.Name.Contains(q));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => MapToResponse(l))
            .ToListAsync();

        return new PagedResult<LeadListResponse>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<LeadListResponse?> GetById(Guid id)
    {
        var leadList = await _context.LeadLists.FindAsync(id);
        if (leadList != null)
            return MapToResponse(leadList);

        _logger.LogError("LeadListAPI with id {id} was not found", id);
        return null;
    }

    public async Task<(LeadListResponse? Response, string? ErrorMessage)> Create(LeadListCreateRequest request)
    {
        var leadList = new LeadList
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            SourceUrl = request.SourceUrl.Trim(),
            Status = LeadListStatus.Pending,
            CorrelationId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var msg = new LeadListCreatedMsg
        {
            LeadListId = leadList.Id,
            CorrelationId = leadList.CorrelationId,
            SourceUrl = leadList.SourceUrl,
            CreatedAt = leadList.CreatedAt
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.LeadLists.Add(leadList);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Lead list {LeadListId} created with CorrelationId {CorrelationId}", leadList.Id,
                leadList.CorrelationId);

            await CreateJobAsync(leadList, msg);
            await transaction.CommitAsync();
            return (MapToResponse(leadList), null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create lead list and publish message");
            return (null, "Failed to save the lead list.");
        }
    }

    public async Task<(LeadListResponse? Response, string? ErrorMessage)> Update(Guid id, LeadListCreateRequest request)
    {
        var leadList = await _context.LeadLists.FindAsync(id);

        if (leadList == null)
        {
            _logger.LogError("Lead with id {id} was not found", id);
            return (null, "Lead list not found");
        }

        if (!leadList.IsEditable())
            return (null, $"Cannot update lead list with the status {leadList.Status}. Only Pending or Failed can be updated");

        leadList.Name = request.Name.Trim();
        leadList.SourceUrl = request.SourceUrl.Trim();
        leadList.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Lead list {LeadListId} updated", leadList.Id);

        return (MapToResponse(leadList), null);
    }

    public async Task<(bool Success, string? ErrorMessage)> Delete(Guid id)
    {
        var leadList = await _context.LeadLists.FindAsync(id);
        if (leadList == null)
        {
            _logger.LogError("Lead list with id {id} was not found", id);
            return (false, "Lead not found");
        }

        if (!leadList.IsDeletable())
            return (false, $"Cannot delete lead list with status {leadList.Status}. Only Pending or Failed are allowed.");

        _context.LeadLists.Remove(leadList);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Lead list {LeadListId} deleted", leadList.Id);

        return (true, null);
    }

    public async Task<(LeadListResponse? Response, string? ErrorMessage)> Reprocess(Guid id)
    {
        var leadList = await _context.LeadLists.FindAsync(id);
        if (leadList == null)
        {
            _logger.LogError("Lead list with id {id} was not found", id);
            return (null, "Lead list not found");
        }

        if (leadList.Status != LeadListStatus.Failed)
            return (null, $"Cannot reprocess lead list with status {leadList.Status}. Only Failed list can be reprocessed. ");

        leadList.Status = LeadListStatus.Pending;
        leadList.ProcessedCount = 0;
        leadList.ErrorMessage = null;
        leadList.CorrelationId = Guid.NewGuid();
        leadList.UpdatedAt = DateTime.UtcNow;
        
        var msg = new LeadListCreatedMsg
        {
            LeadListId = leadList.Id,
            CorrelationId = leadList.CorrelationId,
            SourceUrl = leadList.SourceUrl,
            CreatedAt = leadList.CreatedAt
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Lead list {LeadListId} marked for reprocessing", leadList.Id);

            await CreateJobAsync(leadList, msg);
            await transaction.CommitAsync();
            return (MapToResponse(leadList), null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to put reprocess lead list and publish message");
            return (null, "Failed to save the lead list.");
        }
    }
    
    private async Task CreateJobAsync(LeadList leadList, LeadListCreatedMsg msg)
    {
        // await _rabbitMqPublisher.PublishLeadListCreated(msg);
        await _jobCreator.CreateWorkerJobAsync(leadList.Id, leadList.CorrelationId);
    }
    
    private static LeadListResponse MapToResponse(LeadList leadList) =>
        new()
        {
            Id = leadList.Id,
            Name = leadList.Name,
            SourceUrl = leadList.SourceUrl,
            Status = leadList.Status.ToString(),
            ProcessedCount = leadList.ProcessedCount,
            ErrorMessage = leadList.ErrorMessage,
            CreatedAt = leadList.CreatedAt,
            UpdatedAt = leadList.UpdatedAt,
            CorrelationId = leadList.CorrelationId
        };
}