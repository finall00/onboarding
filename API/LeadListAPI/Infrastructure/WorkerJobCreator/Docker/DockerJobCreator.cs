using leadListAPI.Interfaces;

namespace leadListAPI.Infrastructure.WorkerJobCreator.Docker;

public class DockerJobCreator : IJobCreator
{
    private readonly ILogger<DockerJobCreator> _logger;

    public DockerJobCreator(ILogger<DockerJobCreator> logger)
    {
        _logger = logger;
    }

    public Task CreateWorkerJobAsync(Guid leadListId, Guid correlationId)
    {
        _logger.LogInformation("Docker worker creator not implemented, Skip...");
        return Task.CompletedTask;
    }
}