using API.Interfaces;
using k8s;
using k8s.Exceptions;
using k8s.Models;

namespace API.Infrastructure.Kubernetes;

public class KubernetesJobService : IKubernetesJobService
{
    private readonly k8s.Kubernetes _client;
    private readonly ILogger<KubernetesJobService> _logger;
    private const string NameSpace = "dev";

    public KubernetesJobService(ILogger<KubernetesJobService> logger)
    {
        _logger = logger;
        
        try
        {
            var config = KubernetesClientConfiguration.InClusterConfig();
            _client = new k8s.Kubernetes(config);
            _logger.LogInformation("Loaded in-cluster Kubernetes configuration.");
        }
        catch (KubeConfigException)
        {
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            _client = new k8s.Kubernetes(config);
            _logger.LogInformation("Loaded local kubeconfig configuration.");
        }
        
    }

    public async Task CreateWorkerJobAsync(Guid leadListId, Guid correlationId)
    {
        var jobName = $"worker-job-{leadListId}";
        _logger.LogInformation("Creating Kubernetes job {jobName}", jobName);

        var job = new V1Job
        {
            Metadata = new V1ObjectMeta
            {
                Name = jobName,
                NamespaceProperty = NameSpace
            },
            Spec = new V1JobSpec
            {
                Template = new V1PodTemplateSpec
                {
                    Spec = new V1PodSpec
                    {
                        RestartPolicy = "Never",
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "worker",
                                Image = "finall00/leadlists-worker:latest",
                                Env = new List<V1EnvVar>
                                {
                                    new("LEADLIST_ID", leadListId.ToString()),
                                    new("CORRELATION_ID", correlationId.ToString()),
                                    new("POSTGRES_HOST", "postgres"),
                                    new("POSTGRES_PORT", "5432"),
                                    new("POSTGRES_DB", "leadlists"),
                                    new("POSTGRES_USER", "postgres"),
                                    new V1EnvVar
                                    {
                                        Name = "POSTGRES_PASSWORD",
                                        ValueFrom = new V1EnvVarSource
                                        {
                                            SecretKeyRef = new V1SecretKeySelector("POSTGRES_PASSWORD",
                                                "postgres-secret")
                                        }
                                    },
                                    new("RABBITMQ_HOST", "rabbitmq"),
                                    new("RABBITMQ_USER", "guest"),
                                    new("RABBITMQ_PASS", "guest")
                                }
                            }
                        }
                    }
                }
            }
        };

        try
        {
            await _client.BatchV1.CreateNamespacedJobAsync(job, NameSpace);
            _logger.LogInformation("Created Kubernetes job {jobName}", jobName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating {jobName}: {Message}", jobName, e.Message);
        }
    }
}