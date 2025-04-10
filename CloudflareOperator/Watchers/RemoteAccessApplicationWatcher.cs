using CloudflareOperator.Clients;
using CloudflareOperator.Clients.Models;
using CloudflareOperator.Entities;
using CloudflareOperator.Services;
using k8s.Models;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

namespace CloudflareOperator.Watchers;

[EntityRbac(typeof(V1AccessApplication), Verbs = RbacVerb.List)]
internal sealed class RemoteAccessApplicationWatcher(
    ILogger<RemoteAccessApplicationWatcher> logger,
    IKubernetesClient client,
    ApiTokenService apiTokenService,
    ICloudflareClient cloudflareClient,
    PolicyService policyService,
    EntityRequeue<V1AccessApplication> requeue
) : WatcherBase
{
    protected override async Task Watch(CancellationToken cancellationToken)
    {
        foreach (var application in await ListApplications(cancellationToken))
        {
            if (application.DeletionTimestamp() is not null)
                continue;

            var (gotToken, apiToken) = await apiTokenService.TryGetApiToken(application.Spec.ApiToken, cancellationToken);

            if (!gotToken)
                continue;

            var remoteApplication = await cloudflareClient.GetApplication(apiToken, application.Spec.AccountId, application.Status.ApplicationId, cancellationToken)
                .GetResponseContent();

            // Application exists
            if (remoteApplication is { Success: true, Result: not null })
            {
                if (!await HasCorrectPolicies(apiToken, application, remoteApplication.Result))
                    requeue(application, TimeSpan.FromMilliseconds(10));

                continue;
            }

            // 11021 = Application does not exist
            if ((remoteApplication?.Errors ?? []).Any(x => x.Code == 11021))
            {
                logger.LogInformation("Application for resource {Name} missing in cloudflare, recreating", application.Name());

                application.Status.Status = Status.Uninitialized;
                application.Status.ApplicationId = string.Empty;
                var e = await client.UpdateStatusAsync(application, cancellationToken);

                requeue(e, TimeSpan.FromMilliseconds(10));
                continue;
            }

            logger.LogWarning("Unknown error occured when querying cloudflare: {@Response}", remoteApplication);
        }

        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
    }

    private async Task<bool> HasCorrectPolicies(string apiToken, V1AccessApplication entity, Application application)
    {
        if (entity.Spec.AccessPolicies.Length == 0)
            return true;

        if (application.Policies?.Length != entity.Spec.AccessPolicies.Length)
        {
            logger.LogInformation("Applications policies missing, expected {@Expected}, got {@Got}", entity.Spec.AccessPolicies, application.Policies);
            return false;
        }

        var policies = await policyService.GetPolicies(apiToken, entity.Spec.AccountId, entity.Spec.AccessPolicies);

        return policies.SequenceEqual(application.Policies.Value.OrderBy(x => x.Precedence).Select(x => x.Id));
    }

    private Task<IList<V1AccessApplication>> ListApplications(CancellationToken cancellationToken)
    {
        return client.ListAsync<V1AccessApplication>(cancellationToken: cancellationToken);
    }
}