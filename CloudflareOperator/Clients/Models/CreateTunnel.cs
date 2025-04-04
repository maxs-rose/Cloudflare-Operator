using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record CreateTunnel(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("tunnel_secret")]
    string? TunnelSecret = null,
    [property: JsonPropertyName("config_src")]
    string ConfigSrc = "cloudflare");