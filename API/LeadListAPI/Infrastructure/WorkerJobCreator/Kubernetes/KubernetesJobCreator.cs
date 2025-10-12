using k8s;
using k8s.Exceptions;
using k8s.Models;
using leadListAPI.Interfaces;

namespace leadListAPI.Infrastructure.WorkerJobCreator.Kubernetes;

public class KubernetesJobCreator : IJobCreator
{
    private readonly k8s.Kubernetes _client;
    private readonly ILogger<KubernetesJobCreator> _logger;
    private readonly string _jobTemplateContent;
    private const string NameSpace = "dev";

    public KubernetesJobCreator(ILogger<KubernetesJobCreator> logger)
    {
        _logger = logger;

        KubernetesClientConfiguration config;
        try
        {
            config = KubernetesClientConfiguration.InClusterConfig();
            _client = new k8s.Kubernetes(config);
            _logger.LogInformation("Using in-cluster config");
            
        }
        catch (KubernetesClientException)
        {
            _logger.LogWarning("Cant Initialize Kubernetes client using in-cluster config");
            config =  KubernetesClientConfiguration.BuildDefaultConfig();
            _client = new k8s.Kubernetes(config);
            _logger.LogInformation("Load from kubeconfig local");
        }
        
        var path = Path.Combine(AppContext.BaseDirectory, "templates", "job-template.yaml");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Worker Job template not found at {path}");

        _jobTemplateContent = File.ReadAllText(path);

        _logger.LogInformation("Template job loaded.");
    }

    public async Task CreateWorkerJobAsync(Guid leadListId, Guid correlationId)
    {
        try
        {
            var finalYaml = _jobTemplateContent
                .Replace("{{leadListsId}}", leadListId.ToString())
                .Replace("{{correlationId}}", correlationId.ToString());

            var job = KubernetesYaml.Deserialize<V1Job>(finalYaml);
            await _client.CreateNamespacedJobAsync(job, NameSpace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating worker job.");
            throw;
        }
    }
}