namespace leadListAPI.Interfaces;

public interface IKubernetesJobService
{
    public Task CreateWorkerJobAsync(Guid leadListId, Guid correlationId);
}