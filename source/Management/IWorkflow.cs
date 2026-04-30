using Newtonsoft.Json.Linq;

namespace LogicApps.Management;

/// <summary>
/// Defines the public contract for a Logic App workflow. Abstracts the workflow allowing consumers to depend on this interface rather than the concrete implementation,
/// which enables mocking in unit tests and alternative implementations.
/// </summary>
public interface IWorkflow
{
    string? FullName { get; }

    string? Id { get; }

    string? Kind { get; }

    string? Location { get; }

    string? Name { get; }

    string? Type { get; }

    JObject? Definition { get; }

    /// <summary>
    /// Get the workflow's trigger instance, loading it from the management API on first access.
    /// </summary>
    /// <returns>The workflow's <see cref="WorkflowTrigger"/> instance.</returns>
    Task<IWorkflowTrigger> GetTriggerAsync();

    /// <summary>
    /// Retrieves workflow runs from the management API. Results are cached in the instance until <see cref="ReloadAsync"/> is called.
    /// </summary>
    /// <returns>List of <see cref="IWorkflowRun"/> instances for this workflow.</returns>
    Task<List<IWorkflowRun>> GetWorkflowRunsAsync();

    /// <summary>
    /// Returns the first workflow run matching the specified correlation ID, or null if none is found.
    /// </summary>
    /// <param name="correlationId">The correlation ID to match against workflow runs.</param>
    /// <returns>The matching <see cref="IWorkflowRun"/>, or null.</returns>
    Task<IWorkflowRun?> GetWorkflowRunByCorrelationIdAsync(string correlationId);

    /// <summary>
    /// Clears cached run data and reloads runs from the management API.
    /// </summary>
    Task ReloadAsync();
}
