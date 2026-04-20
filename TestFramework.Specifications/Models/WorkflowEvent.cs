using System.Diagnostics.CodeAnalysis;

namespace LogicApps.TestFramework.Specifications.Models;

[ExcludeFromCodeCoverage]
public class WorkflowEvent(string stepName, string status, string? type = null)
{
    public string StepName { get; } = stepName;

    public string Status { get; } = status;

    public string Type { get; } = !string.IsNullOrEmpty(type) ? type : "Action";

    public override string ToString() => $"{StepName}|{Status}";
}