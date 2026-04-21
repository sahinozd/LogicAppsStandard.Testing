using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents the set of properties that describe a single repetition of an action within a workflow run, including status, timing, and related metadata.
/// </summary>
/// <remarks>This class provides detailed information about an individual action repetition in a workflow run,
/// such as its execution window, status code, and links to input and output content. It is typically used to inspect or
/// analyze the execution details of repeated workflow actions, such as those in loops or retry policies.</remarks>
public sealed record WorkflowRunDetailsActionRepetitionProperties
{

    [JsonProperty("iterationCount")]
    public int? IterationCount { get; set; }

    [JsonProperty("canResubmit")]
    public bool? CanResubmit { get; set; }

    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("correlation")]
    public Correlation? Correlation { get; set; }

    [JsonProperty("endTime")]
    public DateTime? EndTime { get; set; }

    [JsonProperty("inputsLink")]
    public WorkflowRunDetailsActionContent? InputsLink { get; set; }

    [JsonProperty("outputsLink")]
    public WorkflowRunDetailsActionContent? OutputsLink { get; set; }

    [JsonProperty("repetitionIndexes")]
    public List<RepetitionIndex>? RepetitionIndexes { get; init; }

    [JsonProperty("startTime")]
    public DateTime? StartTime { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("trackingId")]
    public string? TrackingId { get; set; }
}