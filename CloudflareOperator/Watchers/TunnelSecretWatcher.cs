using CloudflareOperator.Entities;
using k8s;
using k8s.Models;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;

namespace CloudflareOperator.Watchers;

[EntityRbac(typeof(V1Secret), Verbs = RbacVerb.Watch)]
[EntityRbac(typeof(V1Tunnel), Verbs = RbacVerb.Get)]
internal sealed class TunnelSecretWatcher(
    ILogger<TunnelSecretWatcher> logger,
    IKubernetesClient client,
    EntityRequeue<V1Tunnel> requeue
) : WatcherBase
{
    protected override async Task Watch(CancellationToken cancellationToken)
    {
        await foreach (var (kind, entity) in client.WatchAsync<V1Secret>("", labelSelector: "strive.io/operator=cloudflared", cancellationToken: cancellationToken))
        {
            if (kind != WatchEventType.Deleted)
                continue;

            var owner = entity.GetOwnerReference(x => x.Kind.Equals("Tunnel") && x.ApiVersion.Equals("strive.io/v1"));

            if (owner is null)
                continue;

            //  If the tunnel is deleting just ignore the watch events
            var tunnel = await client.GetAsync<V1Tunnel>(owner.Name, cancellationToken: cancellationToken);
            if (tunnel is null || tunnel.Status.Status != TunnelState.Done || tunnel.DeletionTimestamp() is not null)
                continue;

            logger.LogInformation("Secret for tunnel {Tunnel} deleted, recreating", tunnel.Name());

            tunnel.Status.Status = TunnelState.Created;
            var t = await client.UpdateStatusAsync(tunnel, cancellationToken);
            requeue(t, TimeSpan.FromMilliseconds(10));
        }
    }
}