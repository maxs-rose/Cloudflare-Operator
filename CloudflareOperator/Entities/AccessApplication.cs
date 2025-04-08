using System.Collections.Immutable;
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace CloudflareOperator.Entities;

[EntityScope(EntityScope.Cluster)]
[KubernetesEntity(Group = "strive.io", ApiVersion = "v1", Kind = "AccessApplication")]
public sealed class V1AccessApplication : CustomKubernetesEntity<V1AccessApplication.AccessApplicationSpec, V1AccessApplication.AccessApplicationStatus>
{
    public sealed class AccessApplicationSpec
    {
        [Required] public string Name { get; init; } = string.Empty;
        [Required] public ImmutableArray<string> Domains { get; init; }

        public ImmutableArray<string> AccessPolicies { get; init; } = [];
        public bool VisibleInLauncher { get; init; } = true;
        public string? LogoUrl { get; init; }
    }

    public sealed class AccessApplicationStatus
    {
        public string ApplicationId { get; init; } = string.Empty;
    }
}