using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record CreateTunnelResponse(
    [property: JsonPropertyName("id")] string Id);