using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Generic wrapper type used for paged responses returned by the management API.
/// </summary>
public sealed record Response<T>
{
    [JsonProperty("value")]
    public IEnumerable<T>? Value { get; set; }

    [JsonProperty("nextLink")]
    public string? NextLink { get; set; }
}