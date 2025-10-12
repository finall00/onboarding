using System.Diagnostics;
using leadListAPI.Interfaces;

namespace leadListAPI.Infrastructure.WorkerJobCreator.LocalProcess;

public class LocalProcessJobCreator(ILogger<LocalProcessJobCreator> logger) : IJobCreator
{
    public Task CreateWorkerJobAsync(Guid leadListId, Guid correlationId)
    {
        logger.LogInformation("Using Process Local strategy for create worker job");
        var processStart = new ProcessStartInfo("dotnet", "run --project ../../worker/Worker.csproj")
        {
            Environment =
            {
                ["LEADLIST_ID"] = leadListId.ToString(),
                ["CORRELATION_ID"] = correlationId.ToString()
            },
            UseShellExecute = false,
            CreateNoWindow = true
        };
        try
        {
            Process.Start(processStart);
            logger.LogInformation("running the dotnet run command for the worker");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError("Can´t create worker job {ExMessage}", ex.Message);
            return Task.FromException(ex);
        }
    }
}