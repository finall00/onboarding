using Worker.Domain.Models;
using Worker.Infrastructure.Data;
using Worker.Interfaces;

namespace Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly AppDbContext? _dbContext;
    
    private readonly Guid _targetLeadListId;
    private readonly Guid _targetCorrelationId;
    

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime hostLifetime,
        IRabbitMqPublisher rabbitMqPublisher
    )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _hostLifetime = hostLifetime;
        _rabbitMqPublisher = rabbitMqPublisher;

        var leadListIdStr = Environment.GetEnvironmentVariable("LEADLIST_ID");
        var correlationIdStr = Environment.GetEnvironmentVariable("CORRELATION_ID");

        if (string.IsNullOrEmpty(leadListIdStr) || !Guid.TryParse(leadListIdStr, out _targetLeadListId))
        {
            _logger.LogError("LEADLIST_ID environment variable is invalid or not set");
            _targetLeadListId = Guid.Empty;
        }

        if (string.IsNullOrEmpty(correlationIdStr) || !Guid.TryParse(correlationIdStr, out _targetCorrelationId))
        {
            _logger.LogError("CORRELATION_ID environment variable is invalid or not set");
            _targetCorrelationId = Guid.Empty;
        }
        _dbContext = _scopeFactory.CreateScope().ServiceProvider.GetService<AppDbContext>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_targetLeadListId == Guid.Empty || _targetCorrelationId == Guid.Empty)
        {
            _logger.LogError("LEADLIST_ID or CORRELATION_ID invalid. Shutting down.");
            _hostLifetime.StopApplication();
            return;
        }

        _logger.LogInformation("Worker started LeadListId: {LeadListId}, CorrelationId: {CorrelationId}",
            _targetLeadListId, _targetCorrelationId);

        await using var scope = _scopeFactory.CreateAsyncScope();

        var leadList = await _dbContext.LeadLists.FindAsync([_targetLeadListId], cancellationToken: stoppingToken);
        if (leadList == null)
        {
            _logger.LogError("LeadList {LeadListId} not found in database", _targetLeadListId);
            return;
        }

        try
        {
            await UpdateLeadListStatusAsync(ll => ll.Status = LeadListStatus.Processing, leadList);

            await ProcessLeadListAsync(leadList);
            _logger.LogInformation("Successfully processed message with CorrelationId {CorrelationId}",
                _targetCorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during job execution. Marking LeadList as Failed.");
            await UpdateLeadListStatusAsync(ll => ll.Status = LeadListStatus.Failed, leadList);
            await _rabbitMqPublisher.PublishLeadList(leadList);
        }
        finally
        {
            _logger.LogInformation("Worker finished. Shutting down application.");
            _hostLifetime.StopApplication();
        }
    }

    private async Task ProcessLeadListAsync(LeadList leadList)
    {
        var random = new Random();

        var delaySeconds = random.Next(2, 6);
        _logger.LogInformation("Simulating processing for LeadList ID : {LeadList_ID} delay: {DelaySeconds}",
            leadList.Id, delaySeconds);
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

        if (random.Next(1, 101) <= 20)
        {
            _logger.LogWarning("Simulating failure");
            throw new InvalidOperationException("failure");
        }

        var processedCount = random.Next(10, 501);
        _logger.LogInformation("Processed {ProcessedCount} leads", processedCount);

        await UpdateLeadListStatusAsync(ll =>
        {
            ll.Status = LeadListStatus.Completed;
            ll.ProcessedCount = processedCount;
            ll.ErrorMessage = null;
        }, leadList);
        _logger.LogInformation("LeadList processed successfully - Count: {Count}, Delay: {Delay}", processedCount,
            delaySeconds);
    }

    private async Task UpdateLeadListStatusAsync(Action<LeadList> updateAction, LeadList leadList)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            updateAction(leadList);
            leadList.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Status updated in database - LeadListId: {LeadListId}, Status: {Status}",
                leadList.Id,
                leadList.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating LeadList status in database");
        }
    }
}