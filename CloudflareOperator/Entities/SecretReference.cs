using KubeOps.Abstractions.Entities.Attributes;

namespace CloudflareOperator.Entities;

public sealed class SecretReference
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string Key { get; set; } = string.Empty;
    public string? Namespace { get; set; }
}