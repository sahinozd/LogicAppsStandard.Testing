using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents a workflow resource (metadata) returned by the management API. Inherits common item fields from <see cref="BaseItem"/>.
/// </summary>
public sealed record Workflow : BaseItem
{
    [JsonProperty("kind")]
    public string? Kind { get; private set; }

    [JsonProperty("location")]
    public string? Location { get; private set; }
}