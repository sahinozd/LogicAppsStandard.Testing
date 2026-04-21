using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents a Logic App resource returned by the management API (model-level DTO). Inherits common identification fields from <see cref="BaseItem"/>.
/// </summary>
public sealed record LogicApp : BaseItem
{
    [JsonProperty("kind")]
    public string? Kind { get; private set; }

    [JsonProperty("location")]
    public string? Location { get; private set; }
}