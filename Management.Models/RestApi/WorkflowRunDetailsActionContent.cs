using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents the content details for an action within a workflow run, including its size, associated metadata, and location URI.
/// </summary>
public sealed record WorkflowRunDetailsActionContent
{
    [JsonProperty("contentSize")]
    public int? ContentSize { get; set; }

    [JsonProperty("metadata")]
    public WorkflowRunDetailsActionContentMetadata? Metadata { get; set; }

    [JsonProperty("uri")]
    public Uri? Uri { get; set; }
}