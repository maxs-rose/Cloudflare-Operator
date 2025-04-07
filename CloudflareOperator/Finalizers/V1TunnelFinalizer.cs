using CloudflareOperator.Clients;
using CloudflareOperator.Entities;
using CloudflareOperator.Services;
using KubeOps.Abstractions.Finalizer;

namespace CloudflareOperator.Finalizers;

internal sealed class V1TunnelFinalizer(
    ApiTokenService apiTokenService,
    ICloudflareClient cloudflareClient,
    DnsService dnsService
) : IEntityFinalizer<V1Tunnel>
{
    public async Task FinalizeAsync(V1Tunnel entity, CancellationToken cancellationToken)
    {
        var (gotToken, apiToken) = await apiTokenService.TryGetApiToken(entity.Spec.ApiToken, cancellationToken);

        if (!gotToken)
            return;

        switch (entity.Status.Status)
        {
            case TunnelState.Done:
            case TunnelState.MissingDns:
            case TunnelState.MissingTunnel:
                await DeleteDnsEntries(entity, apiToken, cancellationToken);

                goto case TunnelState.Created;
            case TunnelState.Created:
                await cloudflareClient.DeleteTunnel(
                    apiToken,
                    entity.Spec.AccountId,
                    entity.Status.TunnelId,
                    cancellationToken);

                goto case TunnelState.Uninitialized;
            case TunnelState.Uninitialized:
                break;
        }
    }

    private async Task DeleteDnsEntries(V1Tunnel entity, string apiToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entity.Status.TunnelId))
            return;

        var tunnelDns = dnsService.TunnelDns(entity.Status.TunnelId);

        await dnsService.DeleteDnsEntries(
            apiToken,
            entity.Spec.AccountId,
            entity.Spec.Dns.Select(d => (d.Hostname, tunnelDns)).ToList(),
            cancellationToken);
    }
}