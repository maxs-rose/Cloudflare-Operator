using CloudflareOperator.Clients;
using CloudflareOperator.Clients.Models;
using CloudflareOperator.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CloudflareOperator.Services;

internal sealed class DnsService(
    ILogger<DnsService> logger,
    ICloudflareClient cloudflareClient,
    IMemoryCache memoryCache)
{
    public string TunnelDns(string tunnelId)
    {
        return $"{tunnelId}.cfargotunnel.com";
    }

    public async Task UpdateTunnelDns(
        string authToken,
        string accountId,
        string tunnelId,
        IList<TunnelDnsEntry> desiredEntries,
        CancellationToken cancellationToken)
    {
        var configuration = await cloudflareClient.GetTunnelConfiguration(
            authToken,
            accountId,
            tunnelId,
            cancellationToken).GetResponseContent();

        if (configuration?.Success is not true)
        {
            logger.LogWarning("Failed to get tunnel configuration: {@Response}", configuration);
            return;
        }

        var existingIngresses = (configuration.Result?.Config?.Ingress ?? [])
            .Where(i => i.Service != "http_status:404")
            .Select(i => i.Hostname + i.Path + i.Service)
            .ToHashSet();
        var targetIngress = desiredEntries.Select(i => i.Hostname + i.Path + i.Service).ToHashSet();

        if (existingIngresses.SetEquals(targetIngress))
            return;

        logger.LogInformation("Updating Tunnel DNS entries for {TunnelId}: {@DnsEntires}", tunnelId, desiredEntries);
        await cloudflareClient.UpdateTunnelConfiguration(
            authToken,
            accountId,
            tunnelId,
            new TunnelConfiguration(new TunnelConfig(
                [
                    ..desiredEntries.Select(x => new TunnelIngress(x.Hostname, x.Service, x.Path)),
                    new TunnelIngress("", "http_status:404", null)
                ]
            )),
            cancellationToken);

        var currentHosts = (configuration.Result?.Config?.Ingress ?? [])
            .Where(x => x.Service != "http_status:404")
            .Select(x => x.Hostname)
            .ToHashSet();

        var tunnelDns = TunnelDns(tunnelId);

        await CreateDnsEntries(
            authToken,
            accountId,
            desiredEntries
                .Select(x => x.Hostname)
                .Except(currentHosts)
                .Select(x => (x, tunnelDns))
                .ToList(),
            cancellationToken);

        await DeleteDnsEntries(
            authToken,
            accountId,
            currentHosts
                .Except(desiredEntries
                    .Select(x => x.Hostname))
                .Select(x => (x, tunnelDns))
                .ToList(),
            cancellationToken);
    }

    public async Task CreateDnsEntries(
        string authToken,
        string accountId,
        IList<(string Name, string Target)> entries,
        CancellationToken cancellationToken)
    {
        foreach (var (name, target) in entries)
        {
            var zone = await GetZone(authToken, accountId, name);

            if (await GetRecordId(authToken, zone, target, name, cancellationToken) is not null)
                continue;

            await cloudflareClient.CreateDnsRecord(
                authToken,
                zone,
                new CreateDns(name, target),
                cancellationToken);
        }
    }

    public async Task DeleteDnsEntries(
        string authToken,
        string accountId,
        IList<(string Name, string Target)> entries,
        CancellationToken cancellationToken)
    {
        foreach (var (name, target) in entries)
        {
            var zone = await GetZone(authToken, accountId, name);
            var id = await GetRecordId(authToken, zone, target, name, cancellationToken);

            if (id is null)
                continue;

            await cloudflareClient.DeleteDnsRecord(
                authToken,
                zone,
                id,
                cancellationToken);
        }
    }

    public Task<string> GetZone(
        string authToken,
        string accountId,
        string hostname)
    {
        var domain = string.Join(".", hostname.Split(".").AsEnumerable().Reverse().Take(2).Reverse());

        return memoryCache.GetOrCreateAsync<string>(domain, async entry =>
        {
            var zones = await cloudflareClient.GetZones(authToken, accountId).GetResponseContent();

            if (zones?.Success is not true)
                return string.Empty;

            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

            return (zones.Result ?? []).FirstOrDefault(z => Equals(domain, StringComparison.InvariantCultureIgnoreCase))?.Id ?? string.Empty;
        })!;
    }

    public async Task<string?> GetRecordId(
        string authToken,
        string zoneId,
        string content,
        string name,
        CancellationToken cancellationToken)
    {
        var result = await cloudflareClient.GetDnsRecords(
            authToken,
            zoneId,
            content,
            cancellationToken).GetResponseContent();

        return result?.Success is not true
            ? null
            : (result.Result ?? []).FirstOrDefault(r => r.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))?.Id;
    }
}