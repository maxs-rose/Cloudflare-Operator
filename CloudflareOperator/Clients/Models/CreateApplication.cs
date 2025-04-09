using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record CreateApplication(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("domain")] string Domain)
{
    [JsonPropertyName("type")] public string Type { get; init; } = "self_hosted";

    [JsonPropertyName("app_launcher_visible")]
    public bool AppLauncherVisible { get; init; }

    [JsonPropertyName("logo_url")] public string? LogoUrl { get; init; }
    [JsonPropertyName("destinations")] public ImmutableArray<ApplicationDestination>? Destinations { get; init; }
}