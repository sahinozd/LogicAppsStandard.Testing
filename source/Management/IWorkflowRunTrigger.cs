using LogicApps.Management.Models.RestApi;
using Newtonsoft.Json.Linq;

namespace LogicApps.Management;

/// <summary>
/// Defines the public contract for the trigger metadata of a single workflow run.
/// Abstracts run-trigger details, enabling consumers to depend on this interface rather than the
/// concrete implementation for testability and alternative implementations.
/// </summary>
public interface IWorkflowRunTrigger
{
    Correlation? Correlation { get; }

    string? DesignerName { get; }

    DateTime? EndTime { get; }

    JToken? Input { get; }

    WorkflowRunDetailsActionContent? InputsLink { get; }

    string? Name { get; }

    string? OriginHistoryName { get; }

    JToken? Output { get; }

    WorkflowRunDetailsActionContent? OutputsLink { get; }

    DateTime? StartTime { get; }

    string? Status { get; }
}
