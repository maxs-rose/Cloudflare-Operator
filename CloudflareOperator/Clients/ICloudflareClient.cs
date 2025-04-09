using CloudflareOperator.Clients.Models;
using Refit;

namespace CloudflareOperator.Clients;

public interface ICloudflareClient
{
    #region Zone

    // https://developers.cloudflare.com/api/resources/zones/

    [Get("/zones")]
    Task<IApiResponse<ResponseEnvelope<List<AccountZones>?>>> GetZones(
        [Header("Authorization")] string authToken,
        [Query] [AliasAs("account.id")] string accountId,
        CancellationToken cancellationToken = default);

    #endregion Zone

    #region Application

    [Post("/accounts/{AccountId}/access/apps")]
    Task<IApiResponse<ResponseEnvelope<Application>>> CreateApplication(
        [Header("Authorization")] string authToken,
        [AliasAs("AccountId")] string accountId,
        [Body] CreateApplication request,
        CancellationToken cancellationToken = default);

    [Put("/accounts/{AccountId}/access/apps/{ApplicationId}")]
    Task<IApiResponse<ResponseEnvelope<Application>>> UpdateApplication(
        [Header("Authorization")] string authToken,
        [AliasAs("AccountId")] string accountId,
        [AliasAs("ApplicationId")] string applicationId,
        [Body] CreateApplication request,
        CancellationToken cancellationToken = default);

    [Get("/accounts/{AccountId}/access/apps/{ApplicationId}")]
    Task<IApiResponse<ResponseEnvelope<Application>>> GetApplication(
        [Header("Authorization")] string authToken,
        [AliasAs("AccountId")] string accountId,
        [AliasAs("ApplicationId")] string applicationId,
        CancellationToken cancellationToken = default);

    [Delete("/accounts/{AccountId}/access/apps/{ApplicationId}")]
    Task<IApiResponse<ResponseEnvelope<Application>>> DeleteApplication(
        [Header("Authorization")] string authToken,
        [AliasAs("AccountId")] string accountId,
        [AliasAs("ApplicationId")] string applicationId,
        CancellationToken cancellationToken = default);

    #endregion Application

    #region Dns

    // https://developers.cloudflare.com/api/resources/dns/subresources/records/

    [Post("/zones/{ZoneId}/dns_records")]
    Task CreateDnsRecord(
        [Header("Authorization")] string authToken,
        [AliasAs("ZoneId")] string zoneId,
        [Body] CreateDns record,
        CancellationToken cancellationToken = default);

    [Delete("/zones/{ZoneId}/dns_records/{Id}")]
    Task<IApiResponse> DeleteDnsRecord(
        [Header("Authorization")] string authToken,
        [AliasAs("ZoneId")] string zoneId,
        [AliasAs("Id")] string id,
        CancellationToken cancellationToken = default);

    [Get("/zones/{ZoneId}/dns_records")]
    Task<IApiResponse<ResponseEnvelope<List<DnsRecord>?>>> GetDnsRecords(
        [Header("Authorization")] string authToken,
        [AliasAs("ZoneId")] string zoneId,
        [Query] [AliasAs("content.exact")] string content,
        CancellationToken cancellationToken = default);

    #endregion Dns

    #region Tunnel

    // https://developers.cloudflare.com/api/resources/zero_trust/subresources/tunnels/

    [Post("/accounts/{AccountId}/cfd_tunnel")]
    Task<IApiResponse<ResponseEnvelope<CreateTunnelResponse>>> CreateTunnel(
        [Header("Authorization")] string authToken,
        [AliasAs("AccountId")] string accountId,
        [Body] CreateTunnel createTunnel,
        CancellationToken cancellationToken = default);

    [Get("/accounts/{AccountId}/cfd_tunnel/{TunnelId}")]
    Task<IApiResponse<ResponseEnvelope<TunnelInfo?>>> GetTunnel(
        [Header("Authorization")] string authToken,
        [AliasAs("AccountId")] string accountId,
        [AliasAs("TunnelId")] string tunnelId,
        CancellationToken cancellationToken = default);

    [Get("/accounts/{AccountId}/cfd_tunnel/{TunnelId}/token")]
    Task<IApiResponse<ResponseEnvelope<string>>> TunnelToken(
        [Header("Authorization")] string authToken,
        [AliasAs("AccountId")] string accountId,
        [AliasAs("TunnelId")] string tunnelId,
        CancellationToken cancellationToken = default);

    [Delete("/accounts/{AccountId}/cfd_tunnel/{TunnelId}")]
    Task<IApiResponse> DeleteTunnel(
        [Header("Authorization")] string authToken,
        [AliasAs("AccountId")] string accountId,
        [AliasAs("TunnelId")] string tunnelId,
        CancellationToken cancellationToken = default);

    [Get("/accounts/{AccountId}/cfd_tunnel/{TunnelId}/configurations")]
    Task<IApiResponse<ResponseEnvelope<TunnelConfiguration?>>> GetTunnelConfiguration(
        [Header("Authorization")] string authToken,
        [AliasAs("AccountId")] string accountId,
        [AliasAs("TunnelId")] string tunnelId,
        CancellationToken cancellationToken = default);

    [Put("/accounts/{AccountId}/cfd_tunnel/{TunnelId}/configurations")]
    Task UpdateTunnelConfiguration(
        [Header("Authorization")] string authToken,
        [AliasAs("AccountId")] string accountId,
        [AliasAs("TunnelId")] string tunnelId,
        [Body] TunnelConfiguration config,
        CancellationToken cancellationToken = default);

    #endregion
}

public static class IApiResponseExtensions
{
    private static async Task<T?> GetResponseContent<T>(this IApiResponse<T> response)
    {
        if (response.IsSuccessful)
            return response.Content;

        return await response.Error.GetContentAsAsync<T>();
    }

    public static async Task<T?> GetResponseContent<T>(this Task<IApiResponse<T>> response)
    {
        var result = await response;
        return await result.GetResponseContent();
    }
}