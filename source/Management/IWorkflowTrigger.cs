namespace LogicApps.Management;

/// <summary>
/// Defines the public contract for a Logic App workflow trigger.
/// Abstracts trigger configuration and execution, enabling consumers to depend on this interface
/// rather than the concrete implementation for testability and alternative implementations.
/// </summary>
public interface IWorkflowTrigger
{
    DateTime? ChangedTime { get; }

    DateTime? CreatedTime { get; }

    string? DesignerName { get; }

    string? Id { get; }

    string? LastExecutionTime { get; }

    string? Name { get; }

    string? NextExecutionTime { get; }

    string? ProvisioningState { get; }

    WorkflowTriggerRecurrence? Recurrence { get; }

    string? State { get; }

    Uri? TriggerUrl { get; }

    string? Type { get; }

    /// <summary>
    /// Execute the trigger endpoint for the workflow.
    /// </summary>
    /// <param name="content">Optional request content to post to the trigger endpoint.</param>
    /// <param name="requestHeaders">Optional headers for the request.</param>
    /// <returns>Execution response wrapped in a <see cref="WorkflowTriggerExecutionResponse"/>.</returns>
    Task<WorkflowTriggerExecutionResponse> RunAsync(HttpContent? content, Dictionary<string, string>? requestHeaders = null);
}
