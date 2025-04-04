using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record TunnelConfiguration([property: JsonPropertyName("config")] TunnelConfig Config);