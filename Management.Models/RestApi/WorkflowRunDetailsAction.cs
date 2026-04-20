using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents the details of an action within a workflow run, including its associated properties.
/// </summary>
public sealed record WorkflowRunDetailsAction : BaseItem
{
    [JsonProperty("properties")]
    public WorkflowRunDetailsActionProperties? Properties { get; private set; }
}