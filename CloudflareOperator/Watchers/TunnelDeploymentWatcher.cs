using CloudflareOperator.Entities;
using CloudflareOperator.Services;
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
        foreach (var tunnel in await GetTunnels(cancellationToken))
        {
            if (tunnel.Status.Status != TunnelState.Done)
                continue;

            var deployment = await tunnelDeploymentService.Get(tunnel, tunnel.Spec.ResourceNamespace, cancellationToken);

            if (deployment is not null && deployment.IsOwnedBy(tunnel))
                continue;

            logger.LogInformation("Tunnel deployment {Name} missing, recreating", tunnel.Name());

            tunnel.Status.Status = TunnelState.MissingTunnel;
            var t = await client.UpdateStatusAsync(tunnel, cancellationToken);
            requeue(t, TimeSpan.FromMilliseconds(10));
        }

        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
    }

    private Task<IList<V1Tunnel>> GetTunnels(CancellationToken cancellationToken)
    {
        return client.ListAsync<V1Tunnel>(cancellationToken: cancellationToken);
    }
}