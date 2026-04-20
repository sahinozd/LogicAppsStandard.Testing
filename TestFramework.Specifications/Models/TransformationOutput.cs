using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace LogicApps.TestFramework.Specifications.Models;

[ExcludeFromCodeCoverage]
public record TransformationOutput<T>
{
    [JsonProperty("body")]
    public T? Body { get; set; }
}