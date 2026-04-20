using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents metadata about the action content for a workflow run, including information related to 'foreach' item counts.
/// </summary>
public sealed record WorkflowRunDetailsActionContentMetadata
{
    [JsonProperty("foreachItemsCount")]
    public int? ForeachItemsCount { get; set; }
}