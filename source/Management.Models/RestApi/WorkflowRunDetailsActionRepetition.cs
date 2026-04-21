using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents the details of an action repetition within a workflow run.
/// </summary>
public sealed record WorkflowRunDetailsActionRepetition : BaseItem
{
    [JsonProperty("properties")]
    public WorkflowRunDetailsActionRepetitionProperties? Properties { get; set; }
}