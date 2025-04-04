using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record TunnelConfig(
    [property: JsonPropertyName("ingress")]
    List<TunnelIngress>? Ingress);