using System.Collections.Immutable;
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
        public ImmutableArray<TunnelTarget> Targets { get; init; } = [];
        public string Image { get; init; } = "cloudflare/cloudflared:2025.2.0";

        [Required] public V1SecretKeySelector AccessTokenRef { get; init; } = null!;
        [Required] public V1SecretKeySelector SecretAccessTokenRef { get; init; } = null!;
    }

    public sealed record TunnelTarget([property: Required] string Name, [property: Required] string Host, [property: Required] int Port);
}