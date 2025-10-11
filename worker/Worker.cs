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

    private readonly Guid _targetLeadListId;
    private readonly Guid _targetCorrelationId;
    private const int MessageTimeoutSeconds = 60;

    private bool _messageProcessed = false;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime hostLifetime,
        IRabbitMqConsumer rabbitMqConsumer
       )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _hostLifetime = hostLifetime;
        _rabbitMqConsumer = rabbitMqConsumer;

        var leadListIdStr = Environment.GetEnvironmentVariable("LEADLIST_ID");
        var correlationIdStr = Environment.GetEnvironmentVariable("CORRELATION_ID");

        if (string.IsNullOrEmpty(leadListIdStr) || !Guid.TryParse(leadListIdStr, out _targetLeadListId))
        {
            _logger.LogError("leadListId is invalid");
            _targetLeadListId = Guid.Empty;
        }

        if (string.IsNullOrEmpty(correlationIdStr) || !Guid.TryParse(correlationIdStr, out _targetCorrelationId))
        {
            _logger.LogError("CorrelationId is invalid");
            _targetCorrelationId = Guid.Empty;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_targetLeadListId == Guid.Empty || _targetCorrelationId == Guid.Empty)
        {
            _logger.LogError("FATAL: LEADLIST_ID or CORRELATION_ID are not defined. shutting down.");
            if (_targetLeadListId == Guid.Empty)
            {
                await UpdateStatus(
                    ll => ll.Status = LeadListStatus.Failed,
                    ll => ll.ErrorMessage = "Invalid environment variable");
            }

            _hostLifetime.StopApplication();
            return;
        }

        _logger.LogInformation("Initialize leadListId: {LeadListId} | CorrelationId: {CorrelationId}",
            _targetLeadListId, _targetCorrelationId);

        try
        {
            await UpdateStatus(list => list.Status = LeadListStatus.Processing);
            _logger.LogInformation("Change status to Processing");

            await _rabbitMqConsumer.Connect(stoppingToken);

            await _rabbitMqConsumer.StartConsuming(OnMessageReceived, stoppingToken);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(MessageTimeoutSeconds));

            try
            {
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError("Timeout. message not received in {Timeout} seconds", MessageTimeoutSeconds);

                while (!_messageProcessed && !timeoutCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(100, timeoutCts.Token);
                }
                
                if (!_messageProcessed)
                {
                    throw new TimeoutException($"Message has timed out after {MessageTimeoutSeconds} seconds");
                }
                
                await UpdateStatus(list =>
                    {
                        list.Status = LeadListStatus.Failed;
                        list.ErrorMessage = $"Timeout. message not received in {MessageTimeoutSeconds}";
                    });
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout waiting for message");
            await UpdateStatus(
                list => list.Status = LeadListStatus.Failed,
                list => list.ErrorMessage = ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker error");
            await UpdateStatus(
                list => list.Status = LeadListStatus.Failed,
                list => list.ErrorMessage = ex.Message);
        }
        finally
        {
            _rabbitMqConsumer.Dispose();
            _logger.LogInformation("Worker finished");
            _hostLifetime.StopApplication();
        }
    }

    private async Task<bool> OnMessageReceived(LeadListCreatedMsg message, ulong deliveryTag)
    {
        if (_messageProcessed)
        {
            _logger.LogDebug("message processed");
            await _rabbitMqConsumer.NackMsg(deliveryTag, requeue: true);
            return false;
        }

        if (message.CorrelationId != _targetCorrelationId)
        {
            _logger.LogError("CorrelationId not math. Expecte: {Expected}", _targetCorrelationId);

            await _rabbitMqConsumer.NackMsg(deliveryTag, requeue: true);
            return false;
        }

        _logger.LogInformation("Message received. Processing...");

        try
        {
            await ProcessLeadListAsync(message);

            await _rabbitMqConsumer.AckMsg(deliveryTag, false);
            _messageProcessed = true;

            return true;
        }
        catch (Exception ex)
        {
            await UpdateStatus(
                ll => ll.Status = LeadListStatus.Failed,
                ll => ll.ErrorMessage = ex.Message);

            await _rabbitMqConsumer.NackMsg(deliveryTag, requeue: false);
            _messageProcessed = true;

            return false;
        }
    }

    private async Task UpdateStatus(Action<LeadList> updateAction, Action<LeadList>? secondaryAction = null)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var leadList = await dbContext.LeadLists.FindAsync(_targetLeadListId);
            if (leadList != null)
            {
                updateAction(leadList);
                secondaryAction?.Invoke(leadList);
                leadList.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            _logger.LogError("LeadListId: {LeadListId} not found in database", _targetLeadListId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lead list");
        }
    }


    private async Task ProcessLeadListAsync(LeadListCreatedMsg message)
    {
        var random = new Random();

        var delaySeconds = random.Next(2, 6);
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

        var fail = random.Next(1, 101) <= 20;
        if (fail)
        {
            _logger.LogError("LeadList with fail");
            throw new Exception("deliberate processing failure");
        }

        var processedCount = random.Next(10, 501);
        await UpdateStatus(list =>
        {
            list.Status = LeadListStatus.Completed;
            list.ProcessedCount = processedCount;
            list.ErrorMessage = null;
        });

        _logger.LogInformation(
            "LeadList processed with success. ProcessingCount: {processedCount}, Delay: {delaySeconds}", processedCount,
            delaySeconds);
    }
}