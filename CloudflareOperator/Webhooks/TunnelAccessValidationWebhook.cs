using CloudflareOperator.Entities;
using k8s.Models;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Web.Webhooks.Admission.Validation;

namespace CloudflareOperator.Webhooks;

[EntityRbac(typeof(V1Secret), Verbs = RbacVerb.Get)]
[ValidationWebhook(typeof(V1TunnelAccess))]
public sealed class TunnelAccessValidationWebhook(
    IKubernetesClient client
) : ValidationWebhook<V1TunnelAccess>
{
    public override Task<ValidationResult> CreateAsync(V1TunnelAccess entity, bool dryRun, CancellationToken cancellationToken)
    {
        return Validate(entity, cancellationToken);
    }

    public override Task<ValidationResult> UpdateAsync(V1TunnelAccess oldEntity, V1TunnelAccess newEntity, bool dryRun, CancellationToken cancellationToken)
    {
        return Validate(newEntity, cancellationToken);
    }

    private async Task<ValidationResult> Validate(V1TunnelAccess entity, CancellationToken cancellationToken)
    {
        if (!await SecretExists(entity.Namespace(), entity.Spec.AccessTokenRef, cancellationToken) || !await SecretExists(entity.Namespace(), entity.Spec.SecretAccessTokenRef, cancellationToken))
            return Fail("Could not find access secret");

        if (entity.Spec.Targets.Length <= 0)
            return Fail("At least one target is required");

        return Success();
    }

    private async Task<bool> SecretExists(string @namespace, V1SecretKeySelector secret, CancellationToken cancellationToken)
    {
        var res = await client.GetAsync<V1Secret>(secret.Name, @namespace, cancellationToken);

        if (res is null)
            return false;

        return res.Data.ContainsKey(secret.Key) || res.StringData.ContainsKey(secret.Key);
    }
}