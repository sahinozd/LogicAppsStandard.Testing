using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace LogicApps.TestFramework.IntegrationTests.Models;

[ExcludeFromCodeCoverage(Justification = "This is a test model used for the transformation test")]
public record Source
{
    [JsonProperty("value")] public string? Value { get; set; }

    [JsonProperty("onclick")] public string? OnClick { get; set; }
}