namespace CloudflareOperator.Entities;

public sealed class TunnelDeployment
{
    public string? Tunnel { get; set; }
    public string? TunnelId { get; set; }

    public List<(string Url, string Port)> Targets { get; set; }
}