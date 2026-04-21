using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents recurrence configuration returned by the management API for a trigger (frequency, interval, schedule and timezone).
/// </summary>
public sealed record Recurrence
{
    [JsonProperty("frequency")]
    public string? Frequency { get; set; }

    [JsonProperty("interval")]
    public int? Interval { get; set; }

    [JsonProperty("schedule")]
    public TriggerSchedule? Schedule { get; set; }

    [JsonProperty("timeZone")]
    public string? TimeZone { get; set; }
}