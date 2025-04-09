using CloudflareOperator.Entities;
using CloudflareOperator.Services;
using k8s.Models;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Rbac;

namespace CloudflareOperator.Controllers;

[EntityRbac(typeof(V1TunnelAccess), Verbs = RbacVerb.All)]
internal sealed class TunnelAccessController(
    ILogger<TunnelAccessController> logger,
    TunnelDeploymentService deploymentService
) : IEntityController<V1TunnelAccess>
{
    public async Task ReconcileAsync(V1TunnelAccess entity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reconciling {@Access}", entity);

        foreach (var target in entity.Spec.Targets)
            await deploymentService.DeployAccess(
                entity,
                $"{entity.Name()}-{target.Name}",
                entity.Namespace(),
                "cloudflare/cloudflared:2025.2.0",
                target,
                entity.Spec.AccessTokenRef,
                entity.Spec.SecretAccessTokenRef,
                cancellationToken);
    }

    public Task DeletedAsync(V1TunnelAccess entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}