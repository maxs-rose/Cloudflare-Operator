using CloudflareOperator.Clients;
using CloudflareOperator.Entities;
using CloudflareOperator.Services;
using k8s.Models;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

namespace CloudflareOperator.Watchers;

[EntityRbac(typeof(V1Secret), Verbs = RbacVerb.Get)]
[EntityRbac(typeof(V1Tunnel), Verbs = RbacVerb.List | RbacVerb.Update)]
internal sealed class RemoteTunnelWatcher(
    ILogger<RemoteTunnelWatcher> logger,
    ICloudflareClient cloudflared,
    IKubernetesClient client,
    EntityRequeue<V1Tunnel> requeue,
    ApiTokenService apiTokenService
) : WatcherBase
{
    protected override async Task Watch(CancellationToken cancellationToken)
    {
        foreach (var tunnel in await client.ListAsync<V1Tunnel>(cancellationToken: cancellationToken))
        {
            if (tunnel.Status.Status != TunnelState.Done)
                continue;

            var (gotToken, apiToken) = await apiTokenService.TryGetApiToken(tunnel.Spec.ApiToken, cancellationToken);

            if (!gotToken)
            {
                logger.LogWarning("Failed to fetch cloudflare API token");
                continue;
            }

            var tunnelInfo = await cloudflared.GetTunnel(
                    apiToken,
                    tunnel.Spec.AccountId,
                    tunnel.Status.TunnelId,
                    cancellationToken)
                .GetResponseContent();

            if (tunnelInfo?.Result is { DeletedAt: null })
                continue;

            logger.LogWarning(
                "Remote tunnel missing for {Tunnel} ({ExpectedId}), re-creating",
                tunnel.Name(),
                tunnel.Status.TunnelId);

            tunnel.Status.Status = TunnelState.Uninitialized;
            var t = await client.UpdateStatusAsync(tunnel, cancellationToken);
            requeue(t, TimeSpan.FromMilliseconds(10));
        }

        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
    }
}