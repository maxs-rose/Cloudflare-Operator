using CloudflareOperator.Entities;
using k8s;
using k8s.Models;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

namespace CloudflareOperator.Watchers;

[EntityRbac(typeof(V1Service), Verbs = RbacVerb.Watch)]
[EntityRbac(typeof(V1TunnelAccess), Verbs = RbacVerb.Get)]
internal sealed class TunnelAccessServiceWatcher(IKubernetesClient client, EntityRequeue<V1TunnelAccess> requeue) : WatcherBase
{
    protected override async Task Watch(CancellationToken cancellationToken)
    {
        await foreach (var (kind, deployment) in client.WatchAsync<V1Service>("", labelSelector: "strive.io/operator=cloudflared", cancellationToken: cancellationToken))
        {
            if (kind != WatchEventType.Deleted)
                continue;

            var owner = deployment.GetOwnerReference(x => x.Kind.Equals("TunnelAccess") && x.ApiVersion.Equals("strive.io/v1"));

            if (owner is null)
                continue;

            //  If the tunnel is deleting just ignore the watch events
            var tunnel = await client.GetAsync<V1TunnelAccess>(owner.Name, cancellationToken: cancellationToken);
            if (tunnel is null || tunnel.DeletionTimestamp() is not null)
                continue;

            requeue(tunnel, TimeSpan.FromMilliseconds(10));
        }
    }
}