using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record Application(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("domain")] string Domain,
    [property: JsonPropertyName("type")] string Type)
{
    [JsonPropertyName("app_launcher_visible")]
    public bool AppLauncherVisible { get; init; }

    [JsonPropertyName("logo_url")] public string? LogoUrl { get; init; }
    [JsonPropertyName("destinations")] public ImmutableArray<ApplicationDestination>? Destinations { get; init; }
    [JsonPropertyName("policies")] public ImmutableArray<ApplicationAccessPolicy>? Policies { get; init; }
}