using CloudflareOperator.Clients;
using CloudflareOperator.Clients.Models;
using CloudflareOperator.Entities;
using CloudflareOperator.Finalizers;
using CloudflareOperator.Services;
using k8s.Models;
using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Finalizer;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

namespace CloudflareOperator.Controllers;

[EntityRbac(typeof(V1Secret), Verbs = RbacVerb.Get | RbacVerb.Create | RbacVerb.Delete | RbacVerb.Update)]
[EntityRbac(typeof(V1Tunnel), Verbs = RbacVerb.All)]
internal sealed class TunnelController(
    ILogger<TunnelController> logger,
    IKubernetesClient client,
    ICloudflareClient cloudflareClient,
    ApiTokenService apiTokenService,
    DnsService dnsService,
    TunnelDeploymentService tunnelDeploymentService,
    EntityRequeue<V1Tunnel> requeue,
    EntityFinalizerAttacher<V1TunnelFinalizer, V1Tunnel> finalizerAttacher
) : IEntityController<V1Tunnel>
{
    public async Task ReconcileAsync(V1Tunnel entity, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Reconciling {@Tunnel}", entity);

            var (gotToken, apiToken) = await apiTokenService.TryGetApiToken(entity.Spec.ApiToken, cancellationToken);

            if (!gotToken)
                return;

            entity = await finalizerAttacher(entity, cancellationToken);

            switch (entity.Status.Status)
            {
                case TunnelState.Uninitialized:
                    await DeleteDnsEntries(entity, apiToken, cancellationToken);

                    await CreateTunnel(entity, apiToken, cancellationToken);

                    entity = await client.UpdateStatusAsync(entity, cancellationToken);
                    requeue(entity, TimeSpan.FromMilliseconds(10));
                    break;
                case TunnelState.Created:
                    var authToken = await cloudflareClient.TunnelToken(
                        apiToken,
                        entity.Spec.AccountId,
                        entity.Status.TunnelId,
                        cancellationToken).GetResponseContent();

                    await CreateConnectionSecret(
                        entity,
                        authToken?.Result ?? string.Empty,
                        cancellationToken);

                    entity = await client.UpdateStatusAsync(entity, cancellationToken);
                    requeue(entity, TimeSpan.FromMilliseconds(10));
                    break;
                case TunnelState.Deploying:
                    await DeployTunnel(entity, cancellationToken);

                    entity = await client.UpdateStatusAsync(entity, cancellationToken);
                    requeue(entity, TimeSpan.FromMilliseconds(10));
                    break;
                case TunnelState.Done:
                    await dnsService.UpdateTunnelDns(
                        apiToken,
                        entity.Spec.AccountId,
                        entity.Status.TunnelId,
                        entity.Spec.Dns,
                        cancellationToken);
                    break;
                case TunnelState.MissingDns:
                    var tunnelHost = dnsService.TunnelDns(entity.Status.TunnelId);

                    await dnsService.CreateDnsEntries(
                        apiToken,
                        entity.Spec.AccountId,
                        entity.Spec.Dns.Select(x => (x.Hostname, tunnelHost)).ToList(),
                        cancellationToken);

                    entity.Status.Status = TunnelState.Done;
                    entity = await client.UpdateStatusAsync(entity, cancellationToken);
                    break;
                case TunnelState.MissingTunnel:
                    await DeployTunnel(entity, cancellationToken);

                    entity.Status.Status = TunnelState.Done;
                    entity = await client.UpdateStatusAsync(entity, cancellationToken);
                    break;
            }
        }
        catch
        {
            requeue(entity, TimeSpan.FromMilliseconds(10));

            throw;
        }
    }

    public Task DeletedAsync(V1Tunnel entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task CreateTunnel(V1Tunnel entity, string apiToken, CancellationToken cancellationToken)
    {
        var tunnelSpec = entity.Spec;

        var creationResult = await cloudflareClient.CreateTunnel(
                apiToken,
                tunnelSpec.AccountId,
                new CreateTunnel(tunnelSpec.Name),
                cancellationToken)
            .GetResponseContent();

        logger.Log(
            creationResult?.Success ?? false ? LogLevel.Information : LogLevel.Error,
            "Tunnel creation result: {@Result}",
            creationResult);

        entity.Status.TunnelId = creationResult?.Success == true ? creationResult.Result.Id : string.Empty;
        entity.Status.Messages = (creationResult?.Errors ?? creationResult?.Messages ?? []).Select(x => x.Message).ToList();
        entity.Status.Status = creationResult?.Success == true ? TunnelState.Created : TunnelState.Uninitialized;
    }

    private async Task CreateConnectionSecret(V1Tunnel entity, string apiKey, CancellationToken cancellationToken)
    {
        await client.SaveAsync(
            new V1Secret
            {
                Kind = V1Secret.KubeKind,
                ApiVersion = V1Secret.KubeApiVersion,
                Metadata = new V1ObjectMeta
                {
                    Name = entity.Name(),
                    NamespaceProperty = entity.Spec.ResourceNamespace,
                    OwnerReferences = [entity.CreateOwnerReference()],
                    Labels = new Dictionary<string, string>
                    {
                        ["strive.io/operator"] = "cloudflared"
                    }
                },
                Type = "Opaque",
                StringData = new Dictionary<string, string>
                {
                    ["token"] = apiKey
                }
            },
            cancellationToken);

        entity.Status.Status = TunnelState.Deploying;
        entity.Status.Messages = [];
    }

    private async Task DeployTunnel(V1Tunnel entity, CancellationToken cancellationToken)
    {
        await tunnelDeploymentService.Delete(entity, entity.Spec.ResourceNamespace, cancellationToken);

        await tunnelDeploymentService.Deploy(
            entity,
            TunnelDeploymentService.TunnelKind.Tunnel,
            entity.Spec.ResourceNamespace,
            "cloudflare/cloudflared:2025.2.0",
            new V1SecretKeySelector
            {
                Name = entity.Name(),
                Key = "token",
                Optional = false
            },
            cancellationToken);

        entity.Status.Status = TunnelState.Done;
        entity.Status.Messages = [];
    }

    private async Task DeleteDnsEntries(V1Tunnel entity, string apiToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entity.Status.TunnelId))
            return;

        var tunnelDns = dnsService.TunnelDns(entity.Status.TunnelId);

        await dnsService.DeleteDnsEntries(
            apiToken,
            entity.Spec.AccountId,
            entity.Spec.Dns.Select(d => (d.Hostname, tunnelDns)).ToList(),
            cancellationToken);
    }
}