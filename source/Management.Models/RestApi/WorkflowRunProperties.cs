using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents the properties of a workflow run as returned by the management REST API.
/// This type maps to the JSON payload for workflow run metadata (start/end times, status, correlation info, etc.).
/// </summary>
public sealed record WorkflowRunProperties
{
    [JsonProperty("correlation")]
    public Correlation? Correlation { get; private set; }

    [JsonProperty("endTime")]
    public string? EndTime { get; private set; }

    [JsonProperty("startTime")]
    public string? StartTime { get; private set; }

    [JsonProperty("status")]
    public string? Status { get; private set; }
    
    [JsonProperty("waitEndTime")]
    public string? WaitEndTime { get; private set; }
}