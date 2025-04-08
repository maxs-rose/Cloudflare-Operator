using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace CloudflareOperator.Entities;

[EntityScope(EntityScope.Cluster)]
[KubernetesEntity(Group = "strive.io", ApiVersion = "v1", Kind = "Tunnel")]
public sealed class V1Tunnel : CustomKubernetesEntity<V1Tunnel.TunnelSpec, V1Tunnel.TunnelStatus>
{
    public sealed class TunnelSpec
    {
        [Required]
        [Description("Name of the tunnel")]
        public string Name { get; set; } = string.Empty;

        [Required] public string AccountId { get; set; } = string.Empty;
        [Required] public SecretReference ApiToken { get; set; } = null!;
        [Required] public string ResourceNamespace { get; set; } = string.Empty;

        public List<TunnelDnsEntry> Dns { get; set; } = [];
    }

    public sealed class TunnelStatus
    {
        public TunnelState Status { get; set; } = TunnelState.Uninitialized;
        public List<string> Messages { get; set; } = [];
        public string TunnelId { get; set; } = string.Empty;
    }
}

public enum TunnelState
{
    Uninitialized,
    Created,
    Deploying,
    Done,
    MissingDns,
    MissingTunnel
}