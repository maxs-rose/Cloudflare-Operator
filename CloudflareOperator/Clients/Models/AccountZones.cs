using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record AccountZones(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);