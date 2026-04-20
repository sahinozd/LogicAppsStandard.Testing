using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents correlation identifiers used for tracking (action and client tracking ids) in run metadata.
/// </summary>
public sealed record Correlation
{
    [JsonProperty("actionTrackingId")]
    public string? ActionTrackingId { get; set; }

    [JsonProperty("clientTrackingId")]
    public string? ClientTrackingId { get; set; }
}