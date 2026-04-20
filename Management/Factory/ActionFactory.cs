using LogicApps.Management.Actions;
using LogicApps.Management.Helper;
using LogicApps.Management.Models.Enums;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace LogicApps.Management.Factory;

/// <summary>
/// Factory responsible for creating <see cref="BaseAction"/> instances from JSON nodes (Newtonsoft <see cref="Newtonsoft.Json.Linq.JObject"/>).
/// The factory reads the action "type" and constructs the appropriate concrete action model, recursively creating any nested child actions.
/// </summary>
public class ActionFactory : IActionFactory
{
    private readonly IConfiguration _configuration;
    private readonly IAzureManagementRepository _azureManagementRepository;
    private readonly IActionHelper _actionHelper;

    private string? _workflowName;
    private string? _runId;
    private readonly Dictionary<string, object> _data = [];
    private Dictionary<string, Func<string, JObject, string?, Task<BaseAction>>>? _map;

    /// <summary>
    /// Initializes a new instance of the ActionFactory class with the specified configuration, Azure management
    /// repository, and action helper.
    /// </summary>
    /// <param name="configuration">The configuration settings to be used by the factory. Cannot be null.</param>
    /// <param name="azureManagementRepository">The Azure management repository used for resource operations. Cannot be null.</param>
    /// <param name="actionHelper">The action helper that provides utility methods for actions. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if configuration, azureManagementRepository, or actionHelper is null.</exception>
    public ActionFactory(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionHelper actionHelper)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _azureManagementRepository = azureManagementRepository ?? throw new ArgumentNullException(nameof(azureManagementRepository));
        _actionHelper = actionHelper ?? throw new ArgumentNullException(nameof(actionHelper));

        InitializeMap();
    }

    /// <summary>
    /// Initializes a dictionary-based dispatch table mapping action type names (e.g., "Scope", "ForEach", "Until") to their factory creation methods.
    /// Provides O(1) lookup performance and eliminates large switch statements. Used by <see cref="CreateActionFromJObject"/> to determine
    /// which factory method to invoke based on the action's "type" property in the workflow definition JSON.
    /// </summary>
    /// <remarks>
    /// Supports: Scope, Until, ForEach/Foreach, Switch, If/Condition, and Action (default). Implements Strategy + Factory Pattern.
    /// </remarks>
    private void InitializeMap()
    {
        _map = new Dictionary<string, Func<string, JObject, string?, Task<BaseAction>>>
        {
            ["Scope"] = async (name, jObject, repetitionIndex) => await CreateScope(name, jObject, repetitionIndex).ConfigureAwait(false),
            ["Until"] = async (name, jObject, repetitionIndex) => await CreateUntil(name, jObject, repetitionIndex).ConfigureAwait(false),
            ["ForEach"] = async (name, jObject, repetitionIndex) => await CreateForEach(name, jObject, repetitionIndex).ConfigureAwait(false),
            ["Foreach"] = async (name, jObject, repetitionIndex) => await CreateForEach(name, jObject, repetitionIndex).ConfigureAwait(false),
            ["Switch"] = async (name, jObject, repetitionIndex) => await CreateSwitch(name, jObject, repetitionIndex).ConfigureAwait(false),
            ["If"] = async (name, jObject, repetitionIndex) => await CreateCondition(name, jObject, repetitionIndex).ConfigureAwait(false),
            ["Condition"] = async (name, jObject, repetitionIndex) => await CreateCondition(name, jObject, repetitionIndex).ConfigureAwait(false),
            ["Action"] = async (name, _, repetitionIndex) => await CreateAction(name, repetitionIndex).ConfigureAwait(false)
        };
    }

    /// <summary>
    /// Sets the workflow name and run identifier for the current workflow execution context.
    /// </summary>
    /// <param name="workflowName">The name of the workflow to associate with the current run. Cannot be null or empty.</param>
    /// <param name="runId">The unique identifier for the workflow run. Cannot be null or empty.</param>
    public void SetWorkflowRunProperties(string workflowName, string runId)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowName);
        ArgumentException.ThrowIfNullOrEmpty(runId);

        _workflowName = workflowName;
        _runId = runId;
    }

    /// <summary>
    /// Create a <see cref="BaseAction"/> implementation from a JSON object that represents an action in the workflow definition.
    /// The method inspects the <c>type</c> (or <c>Type</c>) property and dispatches to the corresponding creation helper for container actions. For unknown
    /// types a plain <see cref="Actions.Action"/> is returned.
    /// </summary>
    /// <param name="name">The action name (usually the JSON property name).</param>
    /// <param name="node">The JObject representing the action definition.</param>
    /// <param name="repetitionIndex">Repetition index for this action.</param>
    /// <returns>A concrete <see cref="BaseAction"/> instance representing the action.</returns>
    public Task<BaseAction> CreateActionFromJObject(string name, JObject node, string? repetitionIndex = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(node);

        var type = node["type"]?.ToString()
                   ?? node["Type"]?.ToString()
                   ?? string.Empty;

        if (_map?.TryGetValue(type, out var handler) ?? false)
            return handler(name, node, repetitionIndex);

        return CreateAction(name, repetitionIndex);
    }

    /// <summary>
    /// Create a <see cref="ScopeAction"/> and parse its nested <c>actions</c>.
    /// </summary>
    /// <param name="name">Action name.</param>
    /// <param name="repetitionIndex">Repetition index for this action.</param>
    /// <returns>Constructed <see cref="ScopeAction"/>.</returns>
    private async Task<BaseAction> CreateAction(string name, string? repetitionIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(_workflowName);
        ArgumentNullException.ThrowIfNull(_runId);

        // Create the ScopeAction instance and set its basic properties. The Name is the key taken from the parent JSON object and Type is set to
        // the enum value representing a scope.
        var action = new Actions.Action(name, ActionType.Action);

        if (!string.IsNullOrEmpty(repetitionIndex))
        {
            // Load all the the repetitions for this action and put these in the collection bag the first time we iterate through the action.
            // This way we retrieve the full repetition list once from the API and then we can filter on it locally when we build the iterations for each action definition.
            var allActionRepetitions = await RetrieveAndStoreActionRepetitionsInCollectionBag(action).ConfigureAwait(false);

            // If a repetition index is provided, filter the full list of repetitions for this action to find the ones matching the current index.
            // This allows us to associate the correct historical iteration data with each action definition when building the model.
            var filteredResults = allActionRepetitions.First(r => r.Name?.StartsWith(repetitionIndex, StringComparison.InvariantCulture) ?? false);
            await action.SetActionDetails(_actionHelper, filteredResults).ConfigureAwait(false);
        }
        else
        {
            await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, _workflowName, _runId).ConfigureAwait(false);
        }

        // Return the populated scope action.
        return action;
    }

    /// <summary>
    /// Create a <see cref="ScopeAction"/> and parse its nested <c>actions</c>.
    /// </summary>
    /// <param name="name">Action name.</param>
    /// <param name="node">Action JObject.</param>
    /// <param name="repetitionIndex">Repetition index for this action.</param>
    /// <returns>Constructed <see cref="ScopeAction"/>.</returns>
    private async Task<BaseAction> CreateScope(string name, JObject node, string? repetitionIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(_workflowName);
        ArgumentNullException.ThrowIfNull(_runId);

        // Create the ScopeAction instance and set its basic properties. The Name is the key taken from the parent JSON object and Type is set to
        // the enum value representing a scope.
        var scopeAction = new ScopeAction(name, ActionType.Scope);

        if (!string.IsNullOrEmpty(repetitionIndex))
        {
            // Load all the the repetitions for this action and put these in the collection bag the first time we iterate through the action.
            // This way we retrieve the full repetition list once from the API and then we can filter on it locally when we build the iterations for each action definition.
            var allActionRepetitions = await RetrieveAndStoreActionRepetitionsInCollectionBag(scopeAction).ConfigureAwait(false);

            // If a repetition index is provided, filter the full list of repetitions for this action to find the ones matching the current index.
            // This allows us to associate the correct historical iteration data with each action definition when building the model.
            var filteredResults = allActionRepetitions.First(r => r.Name?.StartsWith(repetitionIndex, StringComparison.InvariantCulture) ?? false);
            await scopeAction.SetActionDetails(_actionHelper, filteredResults).ConfigureAwait(false);
        }
        else
        {
            await scopeAction.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, _workflowName, _runId).ConfigureAwait(false);
        }

        // The scope's child actions are stored under an "actions" property which should be a JSON object. If the property is missing or not an
        // object we return the empty scope (no children).
        if (node["actions"] is not JObject actions)
        {
            return scopeAction;
        }

        // Iterate over each named child action inside the `actions` object.  This is done by BuildChildrenAsync.
        var children = await BuildChildrenAsync(actions, repetitionIndex).ConfigureAwait(false);
        scopeAction.Actions.AddRange(children);

        // Return the populated scope action.
        return scopeAction;
    }

    /// <summary>
    /// Create an <see cref="UntilAction"/> and populate its <c>ActionDefinitions</c>
    /// from the JSON <c>actions</c> object.
    /// </summary>
    /// <param name="name">Action name.</param>
    /// <param name="node">Action JObject.</param>
    /// <param name="repetitionIndex">Repetition index for this action.</param>
    /// <returns>Constructed <see cref="UntilAction"/>.</returns>
    private async Task<BaseAction> CreateUntil(string name, JObject node, string? repetitionIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(_workflowName);
        ArgumentNullException.ThrowIfNull(_runId);

        // Construct the UntilAction model. An Until contains declared ActionDefinitions which represent the loop body that executes for each element in the enumeration.
        var untilAction = new UntilAction(name, ActionType.Until);
        await untilAction.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, _workflowName, _runId).ConfigureAwait(false);

        // Load all the the repetitions for this action and put these in the collection bag the first time we iterate through the action.
        // This way we retrieve the full repetition list once from the API and then we can filter on it locally when we build the iterations for each action definition.
        var allUntilActionRepetitions = await RetrieveAndStoreUntilActionRepetitionsInCollectionBag(untilAction, repetitionIndex).ConfigureAwait(false);

        // If a repetition index is provided, filter the full list of repetitions for this action to find the ones matching the current index.
        // This allows us to associate the correct historical iteration data with each action definition when building the model.
        if (repetitionIndex != null)
        {
            var filteredResults = allUntilActionRepetitions.Where(r => r.Name?.StartsWith(repetitionIndex, StringComparison.InvariantCulture) ?? false).ToList();
            if (filteredResults is { Count: > 0 })
            {
                untilAction.Repetitions.AddRange(filteredResults);
            }
        }
        else
        {
            untilAction.Repetitions.AddRange(allUntilActionRepetitions);
        }

        if (node["actions"] is not JObject actions)
        {
            return untilAction;
        }

        const int maxConcurrency = 6;
        await Parallel.ForEachAsync(untilAction.Repetitions, new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency }, async (iteration, _) =>
            {
                // Skip if actions already populated (avoid duplicates)
                if (iteration.Actions.Count > 0)
                {
                    return;
                }

                var children = await BuildChildrenAsync(actions, iteration.Name).ConfigureAwait(false);
                iteration.Actions.AddRange(children);
            }).ConfigureAwait(false);

        return untilAction;
    }

    /// <summary>
    /// Create a <see cref="ForEachAction"/> and populate its <c>ActionDefinitions</c>
    /// from the JSON <c>actions</c> object.
    /// </summary>
    /// <param name="name">Action name.</param>
    /// <param name="node">Action JObject.</param>
    /// <param name="repetitionIndex"></param>
    /// <returns>Constructed <see cref="ForEachAction"/>.</returns>
    private async Task<BaseAction> CreateForEach(string name, JObject node, string? repetitionIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(_workflowName);
        ArgumentNullException.ThrowIfNull(_runId);

        // Construct the ForEachAction model. A ForEach contains declared ActionDefinitions which represent the loop body that executes for each element in the enumeration.
        var forEachAction = new ForEachAction(name, ActionType.ForEach);
        await forEachAction.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, _workflowName, _runId).ConfigureAwait(false);

        // Load all the the repetitions for this action and put these in the collection bag the first time we iterate through the action.
        // This way we retrieve the full repetition list once from the API and then we can filter on it locally when we build the iterations for each action definition.
        var allForEachActionRepetitions = await RetrieveAndStoreForeachActionRepetitionsInCollectionBag(forEachAction).ConfigureAwait(false);

        // If a repetition index is provided, filter the full list of repetitions for this action to find the ones matching the current index.
        // This allows us to associate the correct historical iteration data with each action definition when building the model.
        if (repetitionIndex != null)
        {
            var filteredResults = allForEachActionRepetitions.Where(r => r.Name?.StartsWith(repetitionIndex, StringComparison.InvariantCulture) ?? false).ToList();
            if (filteredResults is { Count: > 0 })
            {
                forEachAction.Repetitions.AddRange(filteredResults);
            }
        }
        else
        {
            forEachAction.Repetitions.AddRange(allForEachActionRepetitions);
        }

        if (node["actions"] is not JObject actions)
        {
            return forEachAction;
        }

        const int maxConcurrency = 6;
        await Parallel.ForEachAsync(forEachAction.Repetitions, new ParallelOptions
        {
            MaxDegreeOfParallelism = maxConcurrency
        }, async (iteration, _) =>
        {
            // Skip if actions already populated (avoid duplicates)
            if (iteration.Actions.Count > 0)
            {
                return;
            }

            var children = await BuildChildrenAsync(actions, iteration.Name).ConfigureAwait(false);
            iteration.Actions.AddRange(children);
        }).ConfigureAwait(false);

        return forEachAction;
    }

    /// <summary>
    /// Create a <see cref="SwitchAction"/> including its <see cref="SwitchCase"/> children. The method parses both explicit
    /// <c>cases</c> and the special <c>default</c> branch (if present) and attaches parsed actions.
    /// </summary>
    /// <param name="name">Action name.</param>
    /// <param name="node">Action JObject.</param>
    /// <param name="repetitionIndex">Repetition index for this action.</param>
    /// <returns>Constructed <see cref="SwitchAction"/>.</returns>
    private async Task<BaseAction> CreateSwitch(string name, JObject node, string? repetitionIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(_workflowName);
        ArgumentNullException.ThrowIfNull(_runId);

        // Create the SwitchAction model which will contain multiple cases.
        var switchAction = new SwitchAction(name, ActionType.Switch);
        if (!string.IsNullOrEmpty(repetitionIndex))
        {
            // Load all the the repetitions for this action and put these in the collection bag the first time we iterate through the action.
            // This way we retrieve the full repetition list once from the API and then we can filter on it locally when we build the iterations for each action definition.
            var allActionRepetitions = await RetrieveAndStoreActionRepetitionsInCollectionBag(switchAction).ConfigureAwait(false);

            // If a repetition index is provided, filter the full list of repetitions for this action to find the ones matching the current index.
            // This allows us to associate the correct historical iteration data with each action definition when building the model.
            var filteredResults = allActionRepetitions.First(r => r.Name?.StartsWith(repetitionIndex, StringComparison.InvariantCulture) ?? false);
            await switchAction.SetActionDetails(_actionHelper, filteredResults).ConfigureAwait(false);
        }
        else
        {
            await switchAction.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, _workflowName, _runId).ConfigureAwait(false);
        }

        // Parse explicit cases if present. The `cases` property is an object where each property name is the case label and its value may contain
        // an `actions` object describing actions for that case.
        if (node["cases"] is JObject cases)
        {
            foreach (var caseProperty in cases.Properties())
            {
                // Create a SwitchCase with the case label as its Name.
                var switchCase = new SwitchCase
                {
                    Name = caseProperty.Name
                };

                // The case value can itself be an object which contains an `actions` object.
                // If so, parse each action and add it to the SwitchCase.Actions list.
                if (caseProperty.Value is JObject caseObject)
                {
                    if (caseObject["actions"] is JObject caseObjectActions)
                    {
                        // Iterate over each named child action inside the `actions` object.  This is done by BuildChildrenAsync.
                        var caseChildren = await BuildChildrenAsync(caseObjectActions, repetitionIndex).ConfigureAwait(false);
                        switchCase.Actions.AddRange(caseChildren);
                    }
                }
                // Add the parsed case to the switch's Cases collection.
                switchAction.Cases.Add(switchCase);
            }
        }

        // Parse the optional `default` branch. If a default branch exists and contains actions,
        // treat it as a SwitchCase named "Default" so that callers can treat all branches uniformly.
        if (node["default"] is not JObject defaultBranchJObject || defaultBranchJObject["actions"] is not JObject actions)
        {
            // No default actions present -> return the switch as parsed so far.
            return switchAction;
        }

        // Build the default SwitchCase and populate it with the declared actions found under the default branch's `actions` object.
        var defaultSwitchCase = new SwitchCase
        {
            Name = "Default"
        };

        // Iterate over each named child action inside the `actions` object.  This is done by BuildChildrenAsync.
        var children = await BuildChildrenAsync(actions, repetitionIndex).ConfigureAwait(false);
        defaultSwitchCase.Actions.AddRange(children);
        switchAction.Cases.Add(defaultSwitchCase);

        return switchAction;
    }

    /// <summary>
    /// Create a <see cref="ConditionAction"/> (if/else) and parse the <c>actions</c> (default) and optional <c>else</c> &lt;actions&gt;.
    /// </summary>
    /// <param name="name">Action name.</param>
    /// <param name="node">Action JObject.</param>
    /// <param name="repetitionIndex">Repetition index for this action.</param>
    /// <returns>Constructed <see cref="ConditionAction"/>.</returns>
    private async Task<BaseAction> CreateCondition(string name, JObject node, string? repetitionIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(_workflowName);
        ArgumentNullException.ThrowIfNull(_runId);

        // Create the ConditionAction (if/else) model and set its basic properties. The default (then) actions are usually stored under the
        // top-level `actions` property and the else branch (if present) is under an `else` object which itself contains `actions`.
        var conditionAction = new ConditionAction(name, ActionType.Condition);
        if (!string.IsNullOrEmpty(repetitionIndex))
        {
            // Load all the the repetitions for this action and put these in the collection bag the first time we iterate through the action.
            // This way we retrieve the full repetition list once from the API and then we can filter on it locally when we build the iterations for each action definition.
            var allActionRepetitions = await RetrieveAndStoreActionRepetitionsInCollectionBag(conditionAction).ConfigureAwait(false);

            // If a repetition index is provided, filter the full list of repetitions for this action to find the ones matching the current index.
            // This allows us to associate the correct historical iteration data with each action definition when building the model.
            var filteredResults = allActionRepetitions.First(r => r.Name?.StartsWith(repetitionIndex, StringComparison.InvariantCulture) ?? false);
            await conditionAction.SetActionDetails(_actionHelper, filteredResults).ConfigureAwait(false);
        }
        else
        {
            await conditionAction.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, _workflowName, _runId).ConfigureAwait(false);
        }

        // Parse the default/then actions if present and add them to the DefaultActions collection on the model.
        if (node["actions"] is JObject defaultActions)
        {
            // Iterate over each named child action inside the `actions` object.  This is done by BuildChildrenAsync.
            var defaultChildren = await BuildChildrenAsync(defaultActions, repetitionIndex).ConfigureAwait(false);

            conditionAction.DefaultActions.AddRange(defaultChildren);
        }

        // Parse the optional else branch. The else branch is represented as an object under `else`,
        // and we expect it to contain an `actions` object. If found, parse each action and add it to ElseActions.
        var elseObj = node["else"] as JObject;
        if (elseObj?["actions"] is JObject elseActions)
        {
            // Iterate over each named child action inside the `actions` object.  This is done by BuildChildrenAsync.
            var elseChildren = await BuildChildrenAsync(elseActions, repetitionIndex).ConfigureAwait(false);
            conditionAction.ElseActions.AddRange(elseChildren);
        }

        return conditionAction;
    }

    /// <summary>
    /// Asynchronously creates and orders a list of child actions from the specified JSON object.
    /// </summary>
    /// <remarks>Each property in the JSON object is converted into a child action. The resulting list is ordered by the start time of each action.</remarks>
    /// <param name="actions">A JSON object containing action definitions, where each property represents a child action to be created.</param>
    /// <param name="repetitionIndex">An optional string representing the repetition index to be applied to each child action. May be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of child actions ordered by
    /// their start time.</returns>
    private async Task<List<BaseAction>> BuildChildrenAsync(JObject actions, string? repetitionIndex)
    {
        var tasks = actions.Properties()
            .Select(p => CreateActionFromJObject(p.Name, (JObject)p.Value, repetitionIndex));

        return (await Task.WhenAll(tasks).ConfigureAwait(false))
            .OrderBy(a => a.StartTime)
            .ToList();
    }

    /// <summary>
    /// Retrieve repetitions for a ForEach action and store them in the internal collection bag for reuse.
    /// Returns cached results when available to avoid repeated API calls.
    /// </summary>
    /// <param name="forEachAction">The ForEach action for which repetitions are requested.</param>
    /// <returns>List of <see cref="ForEachActionRepetition"/> instances.</returns>
    private async Task<List<ForEachActionRepetition>> RetrieveAndStoreForeachActionRepetitionsInCollectionBag(ForEachAction forEachAction)
    {
        ArgumentException.ThrowIfNullOrEmpty(_workflowName);
        ArgumentNullException.ThrowIfNull(_runId);

        var actionRepetitions = GetFromCollectionBag<ForEachActionRepetition>(forEachAction.Name);
        if (actionRepetitions == null || actionRepetitions.Count == 0)
        {
            actionRepetitions = await forEachAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, _workflowName, _runId).ConfigureAwait(false);
            AddToCollectionBag(forEachAction.Name, actionRepetitions);

            forEachAction.RepetitionCount ??= actionRepetitions.Count;
        }

        return actionRepetitions;
    }

    /// <summary>
    /// Retrieve repetitions for an Until action and append them to the internal collection bag. This method will generate
    /// or fetch repetition information and add it to the bag for later lookup.
    /// </summary>
    /// <param name="untilAction">The Until action instance.</param>
    /// <param name="repetitionIndex">Optional repetition index to filter or derive repetitions.</param>
    /// <returns>List of <see cref="UntilActionRepetition"/> instances.</returns>
    private async Task<List<UntilActionRepetition>> RetrieveAndStoreUntilActionRepetitionsInCollectionBag(UntilAction untilAction, string? repetitionIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(_workflowName);
        ArgumentNullException.ThrowIfNull(_runId);

        var actionRepetitions = await untilAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, _workflowName, _runId, repetitionIndex).ConfigureAwait(false);
        AppendToCollectionBag(untilAction.Name, actionRepetitions);

        return actionRepetitions;
    }

    /// <summary>
    /// Retrieves all repetitions of the specified action for the current workflow run and stores them in the collection bag if not already present.
    /// </summary>
    /// <remarks>If the repetitions for the specified action have already been retrieved and stored, this method returns the cached results.
    /// Otherwise, it fetches the repetitions and stores them for future use.</remarks>
    /// <param name="action">The action for which to retrieve repetitions. Must not be null.</param>
    /// <returns>A list of workflow run action details representing all repetitions of the specified action. The list may be empty if no repetitions are found.</returns>
    private async Task<List<WorkflowRunDetailsAction>> RetrieveAndStoreActionRepetitionsInCollectionBag(BaseAction action)
    {
        ArgumentException.ThrowIfNullOrEmpty(_workflowName);
        ArgumentNullException.ThrowIfNull(_runId);

        var actionRepetitions = GetFromCollectionBag<WorkflowRunDetailsAction>(action.Name);
        if (actionRepetitions == null || actionRepetitions.Count == 0)
        {
            actionRepetitions = await action.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, _workflowName, _runId).ConfigureAwait(false);
            AddToCollectionBag(action.Name, actionRepetitions);
        }

        return actionRepetitions;
    }

    /// <summary>
    /// Add a collection into the internal bag keyed by the provided key. Existing entries are replaced.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="key">Dictionary key.</param>
    /// <param name="list">List to store.</param>
    private void AddToCollectionBag<T>(string key, List<T> list)
    {
        _data[key] = list;
    }

    /// <summary>
    /// Append items to an existing list in the internal bag or add a new list if none exists. Ensures resulting list contains distinct items.
    /// </summary>
    private void AppendToCollectionBag<T>(string key, List<T> list)
    {
        if (_data.TryGetValue(key, out var existing) && existing is List<T> existingList)
        {
            existingList.AddRange(list);
            _data[key] = existingList.Distinct().ToList();
            return;
        }

        _data[key] = list.Distinct().ToList();
    }

    /// <summary>
    /// Try to retrieve a previously cached collection from the internal bag by key.
    /// </summary>
    /// <typeparam name="T">Element type expected.</typeparam>
    /// <param name="key">Key used when storing the collection.</param>
    /// <returns>Cached list or null if not found.</returns>
    private List<T>? GetFromCollectionBag<T>(string key)
    {
        if (_data.TryGetValue(key, out var value))
        {
            return (List<T>)value;
        }

        return null;
    }
}