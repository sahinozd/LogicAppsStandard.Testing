using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Wrapper for the properties object returned under a WorkflowRunDetails payload. Contains nested trigger information and potentially other runtime artifacts.
/// </summary>
public sealed record WorkflowRunDetailsProperties
{
    [JsonProperty("trigger")]
    public WorkflowRunDetailsTrigger? Trigger { get; private set; }
}