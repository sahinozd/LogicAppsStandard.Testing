using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents the trigger metadata portion of a <see cref="WorkflowRunDetails"/> payload (inputs/outputs, timings and correlation).
/// </summary>
public sealed record WorkflowRunDetailsTrigger
{
    [JsonProperty("correlation")]
    public Correlation? Correlation { get; set; }

    [JsonProperty("endTime")]
    public DateTime EndTime { get; set; }

    [JsonProperty("inputsLink")]
    public WorkflowRunDetailsActionContent? InputsLink { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("originHistoryName")]
    public string? OriginHistoryName { get; set; }

    [JsonProperty("outputsLink")]
    public WorkflowRunDetailsActionContent? OutputsLink { get; set; }

    [JsonProperty("scheduledTime")]
    public DateTime? ScheduledTime { get; set; }

    [JsonProperty("startTime")]
    public DateTime StartTime { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }
}