using Worker.Domain.DTOs;
using Worker.Domain.Models;
using Worker.Infrastructure.Data;
using Worker.Infrastructure.RabbitMq;

namespace Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IRabbitMqConsumer _rabbitMqConsumer;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    
    private readonly Guid _targetLeadListId;
    private readonly Guid _targetCorrelationId;
    private const int MessageTimeoutSeconds = 30;

    private readonly TaskCompletionSource _messageProcessedT = new();

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime hostLifetime,
        IRabbitMqPublisher rabbitMqPublisher,
        IRabbitMqConsumer rabbitMqConsumer)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _hostLifetime = hostLifetime;
        _rabbitMqConsumer = rabbitMqConsumer;
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
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_targetLeadListId == Guid.Empty || _targetCorrelationId == Guid.Empty)
        {
            _logger.LogError("LEADLIST_ID or CORRELATION_ID invalid. Shutting down.");
            _hostLifetime.StopApplication();
            return;
        }

        _logger.LogInformation("Worker started LeadListId: {LeadListId}, CorrelationId: {CorrelationId}", _targetLeadListId, _targetCorrelationId);

        try
        {
            await _rabbitMqConsumer.ConnectAsync(stoppingToken);

            await UpdateLeadListStatusAsync(ll => ll.Status = LeadListStatus.Processing);

            var consumeResult = await _rabbitMqConsumer.ConsumeMessageAsync(
                _targetCorrelationId,
                TimeSpan.FromSeconds(MessageTimeoutSeconds),
                stoppingToken);

            if (!consumeResult.Found)
                throw new TimeoutException(
                    $"Message with CorrelationId {_targetCorrelationId} not found in queue within {MessageTimeoutSeconds} seconds.");

            await ProcessLeadListAsync(consumeResult.Message!);
            _logger.LogInformation("Successfully processed message with CorrelationId {CorrelationId}", _targetCorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during job execution. Marking LeadList as Failed.");

            await UpdateLeadListStatusAsync(ll =>
            {
                ll.Status = LeadListStatus.Failed;
                ll.ErrorMessage = ex.Message;
            });
        }
        finally
        {
            _rabbitMqConsumer.Dispose();
            _logger.LogInformation("Worker finished. Shutting down application.");
            _hostLifetime.StopApplication();
        }
    }

    private async Task ProcessLeadListAsync(LeadListCreatedMsg message)
    {
        var random = new Random();

        var delaySeconds = random.Next(2, 6);
        _logger.LogInformation("Simulating processing for LeadList ID : {LeadList_ID} delay: {DelaySeconds}",
            delaySeconds, message.LeadListId);
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
        });

        _logger.LogInformation("LeadList processed successfully - Count: {Count}, Delay: {Delay}", processedCount,
            delaySeconds);
    }

    private async Task UpdateLeadListStatusAsync(
        Action<LeadList> updateAction
    )
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var leadList = await dbContext.LeadLists.FindAsync(_targetLeadListId);
            if (leadList == null)
            {
                _logger.LogError("LeadList {LeadListId} not found in database", _targetLeadListId);
                return;
            }

            updateAction(leadList);
            leadList.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

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