using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record TunnelInfo(
    [property: JsonPropertyName("deleted_at")]
    DateTime? DeletedAt);