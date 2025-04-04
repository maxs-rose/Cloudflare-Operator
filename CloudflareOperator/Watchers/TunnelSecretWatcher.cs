using CloudflareOperator.Entities;
using k8s.Models;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

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
        foreach (var tunnel in await GetTunnels(cancellationToken))
        {
            if (tunnel.Status.Status != TunnelState.Done)
                continue;

            var secret = await GetSecret(tunnel.Name(), tunnel.Spec.ResourceNamespace, cancellationToken);

            if (secret is not null && secret.IsOwnedBy(tunnel))
                continue;

            logger.LogInformation("Secret for tunnel {Tunnel} missing", tunnel.Name());

            tunnel.Status.Status = TunnelState.Created;
            var t = await client.UpdateStatusAsync(tunnel, cancellationToken);
            requeue(t, TimeSpan.FromMilliseconds(10));
        }

        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
    }

    private Task<IList<V1Tunnel>> GetTunnels(CancellationToken cancellationToken)
    {
        return client.ListAsync<V1Tunnel>(cancellationToken: cancellationToken);
    }

    private Task<V1Secret?> GetSecret(string name, string @namespace, CancellationToken cancellationToken)
    {
        return client.GetAsync<V1Secret>(name, @namespace, cancellationToken);
    }
}