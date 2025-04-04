using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace CloudflareOperator.Entities;

[EntityScope(EntityScope.Cluster)]
[KubernetesEntity(Group = "strive.io", ApiVersion = "v1", Kind = "Application")]
public sealed class V1Application : CustomKubernetesEntity<V1Application.ApplicationSpec, V1Application.ApplicationStatus>
{
    public sealed class ApplicationSpec
    {
        public string? Name { get; set; }
        public string? LogoUrl { get; set; }
        [Required] public SecretReference ApiToken { get; set; } = null!;
    }

    public sealed class ApplicationStatus
    {
        public ApplicationState Status { get; set; } = ApplicationState.Uninitialized;
        public string ApplicationId { get; set; } = string.Empty;
    }
}

public enum ApplicationState
{
    Uninitialized,
    Done
}