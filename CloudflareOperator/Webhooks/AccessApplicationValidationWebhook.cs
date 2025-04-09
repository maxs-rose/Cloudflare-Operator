using System.Collections.Immutable;
using CloudflareOperator.Entities;
using CloudflareOperator.Services;
using KubeOps.Operator.Web.Webhooks.Admission.Validation;

namespace CloudflareOperator.Webhooks;

[ValidationWebhook(typeof(V1AccessApplication))]
internal sealed class AccessApplicationValidationWebhook(
    ApiTokenService apiTokenService,
    PolicyService policyService
) : ValidationWebhook<V1AccessApplication>
{
    public override Task<ValidationResult> CreateAsync(V1AccessApplication entity, bool dryRun, CancellationToken cancellationToken)
    {
        return Validate(entity, cancellationToken);
    }

    public override Task<ValidationResult> UpdateAsync(V1AccessApplication oldEntity, V1AccessApplication newEntity, bool dryRun, CancellationToken cancellationToken)
    {
        return Validate(newEntity, cancellationToken);
    }

    private async Task<ValidationResult> Validate(V1AccessApplication entity, CancellationToken cancellationToken)
    {
        var (success, apiToken) = await apiTokenService.TryGetApiToken(entity.Spec.ApiToken, cancellationToken);

        if (!success)
            return Fail("Could not find access token");

        if (!await AllPoliciesExist(apiToken, entity.Spec.AccountId, entity.Spec.AccessPolicies))
            return Fail("Access polices do not exist");

        return Success();
    }

    private async Task<bool> AllPoliciesExist(string authToken, string accountId, ImmutableArray<string> policies)
    {
        if (policies.Length == 0)
            return true;

        var result = await Task.WhenAll(policies.Select(p => policyService.HasPolicy(authToken, accountId, p)));

        return result.All(x => x);
    }
}