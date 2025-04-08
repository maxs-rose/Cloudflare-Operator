using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace CloudflareOperator.Entities;

[EntityScope]
[KubernetesEntity(Group = "strive.io", ApiVersion = "v1", Kind = "TunnelAccess")]
public sealed class V1TunnelAccess : CustomKubernetesEntity<V1TunnelAccess.TunnelAccessSpec>
{
    public sealed class TunnelAccessSpec
    {
        public List<TunnelTarget> Targets { get; set; } = [];

        [Required] public V1SecretKeySelector AcessTokenRef { get; set; } = null!;
        [Required] public V1SecretKeySelector SecretAcessTokenRef { get; set; } = null!;
    }

    public sealed record TunnelTarget([property: Required] string Name, [property: Required] string Host, [property: Required] int Port);
}