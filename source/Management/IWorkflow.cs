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
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>List of <see cref="IWorkflowRun"/> instances for this workflow.</returns>
    Task<List<IWorkflowRun>> GetWorkflowRunsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the first workflow run matching the specified correlation ID, or null if none is found.
    /// </summary>
    /// <param name="correlationId">The correlation ID to match against workflow runs.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="IWorkflowRun"/>, or null.</returns>
    Task<IWorkflowRun?> GetWorkflowRunByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the workflow run with the specified Azure-assigned run ID, or null if none is found.
    /// </summary>
    /// <param name="runId">The Azure run ID (the <c>Name</c> field on the run resource, e.g. <c>08585...</c>).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="IWorkflowRun"/>, or null.</returns>
    Task<IWorkflowRun?> GetWorkflowRunByIdAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears cached run data and reloads runs from the management API.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}
