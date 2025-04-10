using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record ApplicationAccessPolicy(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("precedence")]
    int Precedence);