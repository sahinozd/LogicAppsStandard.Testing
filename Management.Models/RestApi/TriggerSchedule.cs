using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents the schedule configuration for a trigger (subset of API fields).
/// </summary>
public sealed record TriggerSchedule
{
    [JsonProperty("hours")]
    public IEnumerable<int>? Hours { get; set; }
}