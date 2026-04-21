using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents a workflow run item returned by the management API. Inherits from <see cref="BaseItem"/> and exposes run-specific properties.
/// </summary>
public sealed record WorkflowRun : BaseItem
{
    [JsonProperty("properties")]
    public WorkflowRunProperties? Properties { get; private set; }
}