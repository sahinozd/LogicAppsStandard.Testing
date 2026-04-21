using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Properties for a trigger resource, including scheduling/recurrence information and execution state.
/// </summary>
public sealed record TriggerProperties : BaseItem
{
    [JsonProperty("changedTime")]
    public DateTime? ChangedTime { get; private set; }

    [JsonProperty("createdTime")]
    public DateTime? CreatedTime { get; private set; }

    [JsonProperty("lastExecutionTime")]
    public string? LastExecutionTime { get; private set; }

    [JsonProperty("nextExecutionTime")]
    public string? NextExecutionTime { get; private set; }

    [JsonProperty("provisioningState")]
    public string? ProvisioningState { get; private set; }

    [JsonProperty("recurrence")]
    public Recurrence? Recurrence { get; private set; }

    [JsonProperty("state")]
    public string? State { get; private set; }
}