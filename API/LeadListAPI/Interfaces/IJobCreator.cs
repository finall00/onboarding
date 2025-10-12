namespace leadListAPI.Interfaces;

public interface IJobCreator
{
    public Task CreateWorkerJobAsync(Guid leadListId, Guid correlationId);
}