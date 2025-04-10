using System.Collections.Immutable;
using CloudflareOperator.Clients;
using CloudflareOperator.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CloudflareOperator.Services;

internal sealed class PolicyService(
    ICloudflareClient cloudflareClient,
    IMemoryCache cache,
    IOptions<CloudflareConfiguration> cloudflareOptions)
{
    private readonly CloudflareConfiguration _cloudflare = cloudflareOptions.Value;

    public async Task<bool> HasPolicy(string authToken, string accountId, string policyName)
    {
        var policies = await GetPolicies(authToken, accountId);

        return policies.ContainsKey(policyName);
    }

    public async Task<ImmutableArray<string>> GetPolicies(string authToken, string accountId, ImmutableArray<string> policyNames)
    {
        if (policyNames.Length == 0)
            return [];

        var polices = await GetPolicies(authToken, accountId);

        return [..policyNames.Select(p => polices[p])];
    }

    private Task<ImmutableDictionary<string, string>> GetPolicies(string authToken, string accountId)
    {
        return cache.GetOrCreateAsync<ImmutableDictionary<string, string>>("policies", async entity =>
        {
            var policies = new Dictionary<string, string>();
            var page = 1;

            while (true)
            {
                var result = await cloudflareClient.GetPolicies(authToken, accountId, page).GetResponseContent();
                page++;

                if (result is not { Success: true, Result: not null })
                    break;

                foreach (var policy in result.Result)
                    policies[policy.Name] = policy.Id;
            }

            entity.SetAbsoluteExpiration(_cloudflare.PolicyCacheTime);

            return policies.ToImmutableDictionary();
        })!;
    }
}