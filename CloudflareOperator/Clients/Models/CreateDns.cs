using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record CreateDns(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("content")]
    string Content,
    [property: JsonPropertyName("proxied")]
    bool Proxied = true,
    [property: JsonPropertyName("type")] string Type = "CNAME");