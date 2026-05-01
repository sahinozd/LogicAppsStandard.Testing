using LogicApps.Management.Actions;

namespace LogicApps.Management;

/// <summary>
/// Defines the public contract for a single Logic App workflow run. Abstracts the workflow run allowing consumers to depend on this interface rather than the concrete implementation,
/// which enables mocking in unit tests and alternative implementations.
/// </summary>
public interface IWorkflowRun
{
    string? ClientTrackingId { get; }

    string? CorrelationId { get; }

    string? EndTime { get; }

    string? Id { get; }

    string? Name { get; }

    string? StartTime { get; }

    string? Status { get; }

    string? Type { get; }

    string? WaitEndTime { get; }

    /// <summary>
    /// Gets the error details for this workflow run, if the run failed.
    /// Returns null when the run succeeded or has not yet completed.
    /// </summary>
    Error? RunError { get; }

    /// <summary>
    /// Get all actions declared in the workflow definition for this run and populate runtime details for each action.
    /// Results are cached for subsequent calls.
    /// </summary>
    /// <returns>List of <see cref="BaseAction"/> instances representing the run's actions.</returns>
    Task<List<BaseAction>> GetWorkflowRunActionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the trigger metadata for this workflow run, loading it from the management API on first access.
    /// </summary>
    /// <returns>The <see cref="WorkflowRunTrigger"/> instance or null if not present.</returns>
    Task<IWorkflowRunTrigger?> GetWorkflowRunTriggerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Find all actions with the specified name in the workflow run, including nested actions.
    /// </summary>
    /// <param name="name">Name of the action to locate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>List of matching <see cref="BaseAction"/> instances, or null if none found.</returns>
    Task<List<BaseAction>?> FindActionByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reload actions and trigger information for this run by clearing cached values and re-fetching from the API.
    /// </summary>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}
