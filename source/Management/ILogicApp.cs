namespace LogicApps.Management;

/// <summary>
/// Defines the public contract for a Logic App Standard resource.
/// Abstracts the top-level Logic App instance, enabling consumers to depend on this interface
/// rather than the concrete implementation for testability and alternative implementations.
/// </summary>
public interface ILogicApp
{
    string? Id { get; }

    string? Kind { get; }

    string? Location { get; }

    string? Name { get; }

    string? Type { get; }

    /// <summary>
    /// Retrieves the list of workflows associated with this Logic App.
    /// Subsequent calls may return cached results until the instance is reloaded.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of <see cref="IWorkflow"/> instances for this Logic App.</returns>
    Task<List<IWorkflow>> GetWorkflowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears cached workflow data and reloads workflows from the management API.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}
