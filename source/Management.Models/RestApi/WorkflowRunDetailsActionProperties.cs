using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents the set of properties that describe the details of an action within a workflow run, including status, timing, error information, and related metadata.
/// </summary>
/// <remarks>This class is typically used to provide detailed information about the execution of a workflow action, such as its start and end times, status, error details,
/// and links to input and output content. It is commonly used in workflow monitoring, diagnostics, and auditing scenarios.</remarks>
public sealed record WorkflowRunDetailsActionProperties
{
    [JsonProperty("canResubmit")]
    public bool? CanResubmit { get; set; }

    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("correlation")]
    public Correlation? Correlation { get; set; }
    
    [JsonProperty("endTime")]
    public DateTime EndTime { get; set; }

    [JsonProperty("error")]
    public Error? Error { get; set; }

    [JsonProperty("inputsLink")]
    public WorkflowRunDetailsActionContent? InputsLink { get; set; }

    [JsonProperty("iterationCount")]
    public int? IterationCount { get; set; }

    [JsonProperty("originHistoryName")]
    public string? OriginHistoryName { get; set; }

    [JsonProperty("outputsLink")]
    public WorkflowRunDetailsActionContent? OutputsLink { get; set; }

    [JsonProperty("repetitionCount")]
    public int? RepetitionCount { get; set; }

    [JsonProperty("scheduledTime")]
    public DateTime? ScheduledTime { get; set; }

    [JsonProperty("startTime")]
    public DateTime StartTime { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("subType")]
    public string? SubType { get; set; }

    [JsonProperty("trackingId")]
    public string? TrackingId { get; set; }
}