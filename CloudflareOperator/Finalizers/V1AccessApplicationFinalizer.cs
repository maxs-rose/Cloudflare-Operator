using CloudflareOperator.Clients;
using CloudflareOperator.Entities;
using CloudflareOperator.Services;
using KubeOps.Abstractions.Finalizer;

namespace CloudflareOperator.Finalizers;

internal sealed class V1AccessApplicationFinalizer(
    ApiTokenService apiTokenService,
    ICloudflareClient cloudflareClient
) : IEntityFinalizer<V1AccessApplication>
{
    public async Task FinalizeAsync(V1AccessApplication entity, CancellationToken cancellationToken)
    {
        if (entity.Status.Status != Status.Done)
            return;

        var (gotToken, apiToken) = await apiTokenService.TryGetApiToken(entity.Spec.ApiToken, cancellationToken);

        if (!gotToken)
            return;

        await cloudflareClient.DeleteApplication(apiToken, entity.Spec.AccountId, entity.Status.ApplicationId, cancellationToken);
    }
}