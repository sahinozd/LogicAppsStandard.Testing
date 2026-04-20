using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents a trigger resource returned by the management API. Inherits common item fields from <see cref="BaseItem"/>.
/// </summary>
public record Trigger : BaseItem
{
    [JsonProperty("properties")]
    public TriggerProperties? Properties { get; private set; }
}