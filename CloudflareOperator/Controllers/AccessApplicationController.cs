using System.Collections.Immutable;
using CloudflareOperator.Clients;
using CloudflareOperator.Clients.Models;
using CloudflareOperator.Entities;
using CloudflareOperator.Extensions;
using CloudflareOperator.Finalizers;
using CloudflareOperator.Services;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Finalizer;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;

namespace CloudflareOperator.Controllers;

[EntityRbac(typeof(V1AccessApplication), Verbs = RbacVerb.All)]
internal sealed class AccessApplicationController(
    ILogger<AccessApplicationController> logger,
    IKubernetesClient client,
    ICloudflareClient cloudflareClient,
    ApiTokenService apiTokenService,
    PolicyService policyService,
    EntityRequeue<V1AccessApplication> requeue,
    EntityFinalizerAttacher<V1AccessApplicationFinalizer, V1AccessApplication> finalizerAttacher
) : IEntityController<V1AccessApplication>
{
    public async Task ReconcileAsync(V1AccessApplication entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling {@Application}", entity);

        try
        {
            var (gotToken, apiToken) = await apiTokenService.TryGetApiToken(entity.Spec.ApiToken, cancellationToken);

            if (!gotToken)
                return;

            switch (entity.Status.Status)
            {
                case Status.Uninitialized:
                    await CreateApplication(entity, apiToken, cancellationToken);

                    var e = await client.UpdateStatusAsync(entity, cancellationToken);
                    await finalizerAttacher(e, cancellationToken);
                    break;
                case Status.Done:
                    await UpdateApplication(entity, apiToken, cancellationToken);
                    return;
            }
        }
        catch
        {
            requeue(entity, TimeSpan.FromMilliseconds(10));
            throw;
        }
    }

    public Task DeletedAsync(V1AccessApplication entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task CreateApplication(V1AccessApplication entity, string apiToken, CancellationToken cancellationToken)
    {
        var (primaryDomain, domains) = entity.Spec.Domains;

        var application = await cloudflareClient.CreateApplication(
                apiToken,
                entity.Spec.AccountId,
                new CreateApplication(entity.Spec.Name, primaryDomain)
                {
                    AppLauncherVisible = entity.Spec.VisibleInLauncher,
                    LogoUrl = entity.Spec.LogoUrl,
                    Destinations = domains.Select(x => new ApplicationDestination(x)).ToImmutableArray(),
                    Policies = await policyService.GetPolicies(apiToken, entity.Spec.AccountId, entity.Spec.AccessPolicies)
                },
                cancellationToken)
            .GetResponseContent();

        if (application is not { Success: true, Result: not null })
        {
            logger.LogWarning("Failed to create application: {@Response}", application);
            requeue(entity, TimeSpan.FromMilliseconds(10));
            return;
        }

        entity.Status.ApplicationId = application.Result.Id;
        entity.Status.Status = Status.Done;
    }

    private async Task UpdateApplication(V1AccessApplication entity, string apiToken, CancellationToken cancellationToken)
    {
        var (primaryDomain, domains) = entity.Spec.Domains;

        var policies = await policyService.GetPolicies(apiToken, entity.Spec.AccountId, entity.Spec.AccessPolicies);

        var application = await cloudflareClient.UpdateApplication(
                apiToken,
                entity.Spec.AccountId,
                entity.Status.ApplicationId,
                new CreateApplication(entity.Spec.Name, primaryDomain)
                {
                    AppLauncherVisible = entity.Spec.VisibleInLauncher,
                    LogoUrl = entity.Spec.LogoUrl,
                    Destinations = domains.Select(x => new ApplicationDestination(x)).ToImmutableArray(),
                    Policies = await policyService.GetPolicies(apiToken, entity.Spec.AccountId, entity.Spec.AccessPolicies)
                },
                cancellationToken)
            .GetResponseContent();

        if (application is not { Success: true, Result: not null })
        {
            logger.LogWarning("Failed to update application: {@Response}", application);
            requeue(entity, TimeSpan.FromMilliseconds(10));
        }
    }
}