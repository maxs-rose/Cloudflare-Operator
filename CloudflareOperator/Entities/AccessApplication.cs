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
        [Required]
        [AdditionalPrinterColumn(name: "Application Name")]
        public string Name { get; init; } = string.Empty;

        [Required] public ImmutableArray<string> Domains { get; init; }

        [Required] public string AccountId { get; init; } = string.Empty;
        [Required] public SecretReference ApiToken { get; init; } = null!;

        public ImmutableArray<string> AccessPolicies { get; init; } = [];
        public bool VisibleInLauncher { get; init; } = true;
        public string? LogoUrl { get; init; }
    }

    public sealed class AccessApplicationStatus
    {
        [AdditionalPrinterColumn(name: "Status")]
        public Status Status { get; set; } = Status.Uninitialized;

        [AdditionalPrinterColumn(name: "Application ID")]
        public string ApplicationId { get; set; } = string.Empty;
    }
}

public enum Status
{
    Uninitialized,
    Done
}