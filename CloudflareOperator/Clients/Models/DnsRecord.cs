using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record DnsRecord(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("content")]
    string Content,
    [property: JsonPropertyName("proxied")]
    bool Proxied,
    [property: JsonPropertyName("type")] string Type);