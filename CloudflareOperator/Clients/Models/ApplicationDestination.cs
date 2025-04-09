using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record ApplicationDestination([property: JsonPropertyName("uri")] string Uri)
{
    [JsonPropertyName("type")] public string Type { get; init; } = "public";
}