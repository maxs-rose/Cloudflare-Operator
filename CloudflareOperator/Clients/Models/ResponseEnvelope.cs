using System.Text.Json.Serialization;

namespace CloudflareOperator.Clients.Models;

public sealed record ResponseEnvelope<T>(
    [property: JsonPropertyName("success")]
    bool Success,
    [property: JsonPropertyName("errors")] ResponseInfo[] Errors,
    [property: JsonPropertyName("messages")]
    ResponseInfo[] Messages,
    [property: JsonPropertyName("result")] T Result);