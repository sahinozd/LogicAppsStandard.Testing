using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents an error payload returned by the management API. Maps the JSON properties "code" and "message" used in API error responses.
/// </summary>
public sealed record Error
{
    [JsonProperty("code")]
    public string? Code { get; private set; }

    [JsonProperty("message")]
    public string? Message { get; private set; }
}