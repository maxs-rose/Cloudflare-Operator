using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients;

public sealed record Policy(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);