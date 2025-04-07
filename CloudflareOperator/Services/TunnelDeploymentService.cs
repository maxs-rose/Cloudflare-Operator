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
    public enum TunnelKind
    {
        Access,
        Tunnel
    }

    private static readonly Dictionary<string, string> Labels = new()
    {
        ["strive.io/operator"] = "cloudflared"
    };

    public async Task Deploy<T>(
        T owner,
        TunnelKind kind,
        string @namespace,
        string image,
        V1SecretKeySelector connectionSecret,
        CancellationToken cancellationToken)
        where T : IKubernetesObject, IMetadata<V1ObjectMeta>
    {
        await client.SaveAsync(
            new V1Deployment
            {
                Kind = V1Deployment.KubeKind,
                ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                Metadata = new V1ObjectMeta
                {
                    Name = owner.Name(),
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
                            Containers = [CreatePodSpec(kind, image, connectionSecret)]
                        }
                    }
                }
            },
            cancellationToken);
    }

    private static V1Container CreatePodSpec(TunnelKind kind, string image, V1SecretKeySelector connectionSecret)
    {
        return kind switch
        {
            TunnelKind.Tunnel => new V1Container
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
            _ => throw new InvalidOperationException($"Unknown Tunnel kind: {kind}")
        };
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

    public async IAsyncEnumerable<(WatchEventType Type, V1Deployment Entity, V1Tunnel Tunnel)> Watch(CancellationToken cancellationToken)
    {
        await foreach (var (kind, deployment) in client.WatchAsync<V1Deployment>("", labelSelector: "strive.io/operator=cloudflared", cancellationToken: cancellationToken))
        {
            var owner = deployment.GetOwnerReference(x => x.Kind.Equals("Tunnel") && x.ApiVersion.Equals("strive.io/v1"));

            if (owner is null)
                continue;

            //  If the tunnel is deleting just ignore the watch events
            var tunnel = await client.GetAsync<V1Tunnel>(owner.Name, cancellationToken: cancellationToken);
            if (tunnel is null || tunnel.Status.Status != TunnelState.Done || tunnel.DeletionTimestamp() is not null)
                continue;

            yield return (kind, deployment, tunnel);
        }
    }
}