using CloudflareOperator.Entities;
using CloudflareOperator.Services;
using k8s;
using k8s.Models;
using KubeOps.Abstractions.Queue;
using KubeOps.KubernetesClient;

namespace CloudflareOperator.Watchers;

public sealed class TunnelDeploymentWatcher(
    ILogger<TunnelDeploymentWatcher> logger,
    IKubernetesClient client,
    TunnelDeploymentService tunnelDeploymentService,
    EntityRequeue<V1Tunnel> requeue
) : WatcherBase
{
    protected override async Task Watch(CancellationToken cancellationToken)
    {
        await foreach (var (type, deployment, tunnel) in tunnelDeploymentService.Watch(cancellationToken))
        {
            if (type != WatchEventType.Deleted)
                continue;

            logger.LogInformation("Tunnel deployment {Name} deleted, recreating", deployment.Name());

            tunnel.Status.Status = TunnelState.MissingTunnel;
            var entity = await client.UpdateStatusAsync(tunnel, cancellationToken);
            requeue(entity, TimeSpan.FromMilliseconds(10));
        }
    }
}