using CloudflareOperator.Entities;
using CloudflareOperator.Services;
using k8s;
using k8s.Models;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;

namespace CloudflareOperator.Watchers;

[EntityRbac(typeof(V1Deployment), Verbs = RbacVerb.Watch)]
[EntityRbac(typeof(V1TunnelAccess), Verbs = RbacVerb.Get)]
internal sealed class TunnelAccessDeploymentWatcher(TunnelDeploymentService deploymentService, EntityRequeue<V1TunnelAccess> requeue) : WatcherBase
{
    protected override async Task Watch(CancellationToken cancellationToken)
    {
        await foreach (var (type, _, tunnel) in deploymentService.WatchAccess(cancellationToken))
        {
            if (type != WatchEventType.Deleted)
                continue;

            requeue(tunnel, TimeSpan.FromMilliseconds(10));
        }
    }
}