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

        [Required] public V1SecretKeySelector AccessTokenRef { get; init; } = null!;
        [Required] public V1SecretKeySelector SecretAccessTokenRef { get; init; } = null!;
    }

    public sealed record TunnelTarget([property: Required] string Name, [property: Required] string Host, [property: Required] int Port);
}

// b94aa7de9708967e0842e3a1a32c6bc6
// 0bc02bd3ce9772ca70756d19f9df91dc78889ac4a1020e004cc8799a73f9cc8f