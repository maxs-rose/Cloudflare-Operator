using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record TunnelIngress(
    [property: JsonPropertyName("hostname")]
    string Hostname,
    [property: JsonPropertyName("service")]
    string Service,
    [property: JsonPropertyName("path")] string? Path);