using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents the top-level workflow run details container returned by the management API. Contains the <see cref="WorkflowRunDetailsProperties"/> payload in the "properties" JSON field.
/// </summary>
public sealed record WorkflowRunDetails
{
    [JsonProperty("properties")]
    public WorkflowRunDetailsProperties? Properties { get; private set; }
}