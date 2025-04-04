using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record ResponseInfo(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")]
    string Message);