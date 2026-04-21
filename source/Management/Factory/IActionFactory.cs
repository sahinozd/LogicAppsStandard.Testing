using LogicApps.Management.Actions;
using Newtonsoft.Json.Linq;

namespace LogicApps.Management.Factory;

public interface IActionFactory
{
    /// <summary>
    /// Create a <see cref="BaseAction"/> implementation from a JSON object that represents an action in the workflow definition.
    /// The method inspects the <c>type</c> (or <c>Type</c>) property and dispatches to the corresponding creation helper for container actions. For unknown
    /// types a plain <see cref="Actions.Action"/> is returned.
    /// </summary>
    /// <param name="name">The action name (usually the JSON property name).</param>
    /// <param name="node">The JObject representing the action definition.</param>
    /// <param name="repetitionIndex">The repetition index for the action</param>
    /// <returns>A concrete <see cref="BaseAction"/> instance representing the action.</returns>
    Task<BaseAction> CreateActionFromJObject(string name, JObject node, string? repetitionIndex = null);

    /// <summary>
    /// Sets the properties for a specific workflow run identified by its name and run ID.
    /// </summary>
    /// <param name="workflowName">The name of the workflow whose run properties are to be set. Cannot be null or empty.</param>
    /// <param name="runId">The unique identifier of the workflow run for which properties will be set. Cannot be null or empty.</param>
    public void SetWorkflowRunProperties(string workflowName, string runId);
}