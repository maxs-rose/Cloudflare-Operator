using CloudflareOperator.Entities;
using CloudflareOperator.Services;
using k8s.Models;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;

namespace CloudflareOperator.Watchers;

[EntityRbac(typeof(V1Tunnel), Verbs = RbacVerb.Get | RbacVerb.Update)]
internal sealed class TunnelDnsWatcher(
    ILogger<TunnelDnsWatcher> logger,
    IKubernetesClient client,
    EntityRequeue<V1Tunnel> requeue,
    DnsService dnsService,
    ApiTokenService apiTokenService
) : WatcherBase
{
    protected override async Task Watch(CancellationToken cancellationToken)
    {
        foreach (var tunnel in await GetTunnels(cancellationToken))
        {
            if (tunnel.Status.Status != TunnelState.Done)
                continue;

            var (gotToken, apiToken) = await apiTokenService.TryGetApiToken(tunnel.Spec.ApiToken, cancellationToken);

            if (!gotToken)
            {
                logger.LogError("Failed to fetch cloudflare API token");
                continue;
            }

            var tunnelDns = dnsService.TunnelDns(tunnel.Status.TunnelId);

            var validRecords = await Task.WhenAll(tunnel.Spec.Dns.Select(x => CheckHost(apiToken, tunnel.Spec.AccountId, tunnelDns, x, cancellationToken)));

            if (validRecords.Length == 0 || validRecords.All(x => x))
                continue;

            logger.LogWarning("Found missing DNS records for {Tunnel}", tunnel.Name());

            tunnel.Status.Status = TunnelState.MissingDns;
            var t = await client.UpdateStatusAsync(tunnel, cancellationToken);
            requeue(t, TimeSpan.FromMilliseconds(10));
        }

        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
    }

    private async Task<bool> CheckHost(
        string apiToken,
        string accountId,
        string content,
        TunnelDnsEntry dnsEntry,
        CancellationToken cancellationToken)
    {
        var zone = await dnsService.GetZone(apiToken, accountId, dnsEntry.Hostname);

        if (string.IsNullOrWhiteSpace(zone))
            return true;

        return await dnsService.GetRecordId(
            apiToken,
            zone,
            content,
            dnsEntry.Hostname,
            cancellationToken) is not null;
    }

    private Task<IList<V1Tunnel>> GetTunnels(CancellationToken cancellationToken)
    {
        return client.ListAsync<V1Tunnel>(cancellationToken: cancellationToken);
    }
}