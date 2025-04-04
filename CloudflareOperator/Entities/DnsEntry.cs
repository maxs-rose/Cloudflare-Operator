using System.ComponentModel.DataAnnotations;

namespace CloudflareOperator.Entities;

public sealed class TunnelDnsEntry
{
    [Required] public string Hostname { get; set; } = string.Empty;
    public string? Path { get; set; }
    [Required] public string Service { get; set; } = string.Empty;
}