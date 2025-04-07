using System.Runtime.CompilerServices;
using CloudflareOperator.Entities;
using k8s;
using k8s.Models;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

namespace CloudflareOperator.Services;

[EntityRbac(typeof(V1Tunnel), Verbs = RbacVerb.Get)]
[EntityRbac(typeof(V1Deployment), Verbs = RbacVerb.Create | RbacVerb.Watch | RbacVerb.Delete)]
public sealed class TunnelDeploymentService(IKubernetesClient client)
{
    private static readonly Dictionary<string, string> Labels = new()
    {
        ["strive.io/operator"] = "cloudflared"
    };

    public async Task DeployTunnel<T>(T owner, string @namespace, string image, V1SecretKeySelector connectionSecret, CancellationToken cancellationToken)
        where T : IKubernetesObject, IMetadata<V1ObjectMeta>
    {
        await CreateDeployment(
            owner,
            owner.Name(),
            @namespace,
            new V1Container
            {
                Name = "tunnel",
                Image = image,
                Args = ["tunnel", "--no-autoupdate", "run", "--token=$(CLOUDFLARE_TOKEN)"],
                Env =
                [
                    new V1EnvVar
                    {
                        Name = "CLOUDFLARE_TOKEN",
                        ValueFrom = new V1EnvVarSource
                        {
                            SecretKeyRef = connectionSecret
                        }
                    }
                ]
            },
            cancellationToken);
    }

    public async Task DeployAccess<T>(
        T owner,
        string name,
        string @namespace,
        string image,
        V1TunnelAccess.TunnelTarget target,
        V1SecretKeySelector tokenRef,
        V1SecretKeySelector secretTokenRef,
        CancellationToken cancellationToken)
        where T : IKubernetesObject, IMetadata<V1ObjectMeta>
    {
        await CreateDeployment(
            owner,
            name,
            @namespace,
            new V1Container
            {
                Name = "tunnel",
                Image = image,
                Args =
                [
                    "access",
                    "tcp",
                    $"--hostname={target.Host}",
                    $"--url=0.0.0.0:{target.Port}",
                    "--service-token-id=$(SERVICE_TOKEN_ID)",
                    "--service-token-secret=$(SERVICE_TOKEN_SECRET)"
                ],
                Ports =
                [
                    new V1ContainerPort
                    {
                        Name = "access",
                        ContainerPort = target.Port,
                        Protocol = "TCP"
                    }
                ],
                Env =
                [
                    new V1EnvVar
                    {
                        Name = "SERVICE_TOKEN_ID",
                        ValueFrom = new V1EnvVarSource
                        {
                            SecretKeyRef = tokenRef
                        }
                    },
                    new V1EnvVar
                    {
                        Name = "SERVICE_TOKEN_SECRET",
                        ValueFrom = new V1EnvVarSource
                        {
                            SecretKeyRef = secretTokenRef
                        }
                    }
                ]
            },
            cancellationToken);
    }

    private Task CreateDeployment<T>(T owner, string name, string @namespace, V1Container container, CancellationToken cancellationToken)
        where T : IKubernetesObject, IMetadata<V1ObjectMeta>
    {
        return client.SaveAsync(
            new V1Deployment
            {
                Kind = V1Deployment.KubeKind,
                ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    NamespaceProperty = @namespace,
                    OwnerReferences = [owner.CreateOwnerReference()],
                    Labels = Labels
                },
                Spec = new V1DeploymentSpec
                {
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = Labels
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = Labels
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = [container]
                        }
                    }
                }
            },
            cancellationToken);
    }

    public async Task Delete<T>(T owner, string @namespace, CancellationToken cancellationToken)
        where T : IKubernetesObject, IMetadata<V1ObjectMeta>
    {
        await client.DeleteAsync(new V1Deployment
            {
                Kind = V1Deployment.KubeKind,
                ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                Metadata = new V1ObjectMeta
                {
                    Name = owner.Name(),
                    NamespaceProperty = @namespace,
                    OwnerReferences = [owner.CreateOwnerReference()],
                    Labels = Labels
                }
            },
            cancellationToken);
    }

    public async IAsyncEnumerable<(WatchEventType Type, V1Deployment Entity, V1Tunnel Tunnel)> WatchTunnel([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var entity in Watch<V1Tunnel>("Tunnel", cancellationToken))
        {
            if (entity.Tunnel.Status.Status != TunnelState.Done)
                continue;

            yield return entity;
        }
    }

    public IAsyncEnumerable<(WatchEventType Type, V1Deployment Entity, V1TunnelAccess Tunnel)> WatchAccess(CancellationToken cancellationToken)
    {
        return Watch<V1TunnelAccess>("TunnelAccess", cancellationToken);
    }

    private async IAsyncEnumerable<(WatchEventType Type, V1Deployment Entity, T Tunnel)> Watch<T>(string ownerType, [EnumeratorCancellation] CancellationToken cancellationToken)
        where T : IKubernetesObject<V1ObjectMeta>
    {
        await foreach (var (kind, deployment) in client.WatchAsync<V1Deployment>("", labelSelector: "strive.io/operator=cloudflared", cancellationToken: cancellationToken))
        {
            var owner = deployment.GetOwnerReference(x => x.Kind.Equals(ownerType) && x.ApiVersion.Equals("strive.io/v1"));

            if (owner is null)
                continue;

            //  If the tunnel is deleting just ignore the watch events
            var tunnel = await client.GetAsync<T>(owner.Name, cancellationToken: cancellationToken);
            if (tunnel is null || tunnel.DeletionTimestamp() is not null)
                continue;

            yield return (kind, deployment, tunnel);
        }
    }
}