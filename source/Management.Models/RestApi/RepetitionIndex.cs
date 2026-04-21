using Newtonsoft.Json;

namespace LogicApps.Management.Models.RestApi;

/// <summary>
/// Represents an index entry for nested repetitions (item index and optional scope name).
/// </summary>
public sealed record RepetitionIndex
{
    [JsonProperty("itemIndex")]
    public int? ItemIndex { get; set; }

    [JsonProperty("scopeName")]
    public string? ScopeName { get; set; }
}