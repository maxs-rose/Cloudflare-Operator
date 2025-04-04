using System.Text;
using CloudflareOperator.Entities;
using k8s.Models;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

namespace CloudflareOperator.Services;

[EntityRbac(typeof(V1Secret), Verbs = RbacVerb.Get)]
internal sealed class ApiTokenService(
    ILogger<ApiTokenService> logger,
    IKubernetesClient client)
{
    public async Task<(bool Success, string Token)> TryGetApiToken(SecretReference secretReference, CancellationToken cancellationToken)
    {
        var secret = await client.GetAsync<V1Secret>(secretReference.Name, secretReference.Namespace, cancellationToken);

        logger.LogDebug("Got Secret: {@Secret}", secret);

        var apiToken = Encoding.UTF8.GetString(secret?.Data[secretReference.Key] ?? []);

        logger.LogDebug("Got api token {ApiToken}", apiToken);

        return (!string.IsNullOrWhiteSpace(apiToken), $"Bearer {apiToken}");
    }
}