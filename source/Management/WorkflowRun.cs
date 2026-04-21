using LogicApps.Management.Actions;
using LogicApps.Management.Factory;
using LogicApps.Management.Helper;
using LogicApps.Management.Models.Constants;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace LogicApps.Management;

/// <summary>
/// Represents a single run of a Logic App workflow and provides methods to load actions and trigger information.
/// </summary>
public sealed class WorkflowRun
{
    private readonly IConfiguration _configuration;
    private readonly IAzureManagementRepository _azureManagementRepository;
    private readonly IActionHelper _actionHelper;
    private readonly IActionFactory _actionFactory;

    private readonly Models.RestApi.WorkflowRun _workflowRunProperties;
    private readonly JObject _workflowDefinition;
    private readonly string _workflowName;

    private List<BaseAction>? _actions;
    private WorkflowRunTrigger? _trigger;
    private string? _correlationId;

    private readonly string _variableActionName;
    private readonly string _correlationIdVariableName;

    public string? ClientTrackingId { get; private set; }

    public string? CorrelationId => _correlationId;

    public string? EndTime { get; private set; }

    public string? Id { get; private set; }

    public string? Name { get; private set; }

    public string? StartTime { get; private set; }

    public string? Status { get; private set; }

    public string? Type { get; private set; }

    public string? WaitEndTime { get; private set; }

    private WorkflowRun(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionFactory actionFactory, IActionHelper actionHelper, string workflowName, Models.RestApi.WorkflowRun workflowRunProperties, JObject workflowDefinition)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _variableActionName = configuration["VariableActionName"] ?? string.Empty;
        _correlationIdVariableName = configuration["CorrelationIdVariableName"] ?? string.Empty;

        _azureManagementRepository = azureManagementRepository ?? throw new ArgumentNullException(nameof(azureManagementRepository));
        _actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
        _actionHelper = actionHelper ?? throw new ArgumentNullException(nameof(actionHelper));
        _workflowName = workflowName ?? throw new ArgumentNullException(nameof(workflowName));
        _workflowRunProperties = workflowRunProperties ?? throw new ArgumentNullException(nameof(workflowRunProperties));
        _workflowDefinition = workflowDefinition ?? throw new ArgumentNullException(nameof(workflowDefinition));
    }

    /// <summary>
    /// Get all actions declared in the workflow definition for this run and populate runtime details for each action.
    /// Results are cached for subsequent calls.
    /// </summary>
    /// <returns>List of <see cref="BaseAction"/> instances representing the run's actions.</returns>
    public async Task<List<BaseAction>> GetWorkflowRunActionsAsync()
    {
        if (_actions is { Count: > 0 })
        {
            return _actions;
        }

        _actions = await BuildActionListFromWorkflowDefinition().ConfigureAwait(false);
        // Sort the children on start time
        _actions = [.. _actions.OrderBy(c => c.StartTime)];

        return _actions;
    }

    /// <summary>
    /// Get the trigger metadata for this workflow run, loading it from the management API on first access.
    /// </summary>
    /// <returns>The <see cref="WorkflowRunTrigger"/> instance or null if not present.</returns>
    public async Task<WorkflowRunTrigger?> GetWorkflowRunTriggerAsync()
    {
        if (_trigger != null)
        {
            return _trigger;
        }
        
        _trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, _workflowName, Name!).ConfigureAwait(false);
        return _trigger;
    }

    /// <summary>
    /// Reload actions and trigger information for this run by clearing cached values and re-fetching from the API.
    /// </summary>
    /// <returns>A task that represents the asynchronous reload operation.</returns>
    public async Task Reload()
    {
        _trigger = null;
        _actions = null;

        await GetWorkflowRunActionsAsync().ConfigureAwait(false);
        await GetWorkflowRunTriggerAsync().ConfigureAwait(false);
    }
   
    /// <summary>
    /// Asynchronously creates and initializes a new workflow run instance using the specified configuration,
    /// repositories, and workflow definition.
    /// </summary>
    /// <param name="configuration">The configuration settings to use for the workflow run.</param>
    /// <param name="azureManagementRepository">The Azure management repository used to interact with Azure resources.</param>
    /// <param name="actionFactory">The factory used to create workflow actions.</param>
    /// <param name="actionHelper">The helper used to assist with workflow action execution.</param>
    /// <param name="workflowName">The name of the workflow to be executed.</param>
    /// <param name="workflowRunProperties">The properties that define the workflow run, including metadata and execution parameters.</param>
    /// <param name="workflowDefinition">The JSON object representing the workflow definition.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the initialized workflow run
    /// instance.</returns>
    public static Task<WorkflowRun> CreateAsync(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionFactory actionFactory, IActionHelper actionHelper, string workflowName, Models.RestApi.WorkflowRun workflowRunProperties, JObject workflowDefinition)
    {
        var ret = new WorkflowRun(configuration, azureManagementRepository, actionFactory, actionHelper, workflowName, workflowRunProperties, workflowDefinition);
        return ret.InitializeAsync();
    }

    /// <summary>
    /// Find the first action with the specified <paramref name="name"/> in the workflow. The search enumerates every action reachable from the top
    /// level by using <see cref="Traverse(BaseAction?)"/> which produces a depth-first, flattened sequence of all actions.
    /// Returns null if no matching action exists.
    /// </summary>
    /// <param name="name">Name of the action to locate. If null or empty the method returns null.</param>
    /// <returns>The matching <see cref="BaseAction"/>, or null if not found.</returns>
    public async Task<List<BaseAction>?> FindActionByNameAsync(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        // We use SelectMany(Traverse) to lazily flatten the tree of actions
        // into a single enumerable. This avoids allocating an intermediate
        // list and allows short-circuiting via FirstOrDefault when a match is
        // found.

        if(_actions == null)
        {
            await GetWorkflowRunActionsAsync().ConfigureAwait(false);
        }

        return _actions?
            .SelectMany(Traverse)
            .Where(a => a.Name == name || a.DesignerName == name).ToList();
    }

    /// <summary>
    /// Initialize the workflow run instance by setting derived properties and retrieving the correlation id.
    /// This method performs any asynchronous initialization needed before the instance is returned to callers.
    /// </summary>
    /// <returns>The initialized <see cref="WorkflowRun"/> instance.</returns>
    private async Task<WorkflowRun> InitializeAsync()
    {
        SetProperties();
        await GetCorrelationIdAsync().ConfigureAwait(false);

        return this;
    }

    /// <summary>
    /// Parse the workflow definition to construct the action model and load details for each action using the action factory.
    /// </summary>
    /// <remarks>Internal helper used by <see cref="GetWorkflowRunActionsAsync"/>.</remarks>
    private async Task<List<BaseAction>> BuildActionListFromWorkflowDefinition()
    {
        List<BaseAction> workflowRunActions = [];

        // The factory needs the workflow name and run id in order to retrieve metadata for the actions, so we set these properties before creating actions.
        _actionFactory.SetWorkflowRunProperties(_workflowName, Name!);

        // Only interested in the 'actions' element
        var actionsToken = _workflowDefinition["definition"]?["actions"] ?? _workflowDefinition["actions"];

        if (actionsToken is not JObject actions)
        {
            throw new InvalidOperationException("Workflow definition does not contain a valid 'actions' section.");
        }

        foreach (var property in actions.Properties())
        {
            if (property.Value is not JObject actionObj)
            {
                continue;
            }

            var action = await _actionFactory.CreateActionFromJObject(property.Name, actionObj).ConfigureAwait(false);
            workflowRunActions.Add(action);
        }

        return workflowRunActions;
    }

    /// <summary>
    /// Retrieve the correlation id for this run by loading the configured initialize variables action and reading
    /// the variable containing the correlation id value from the action inputs.
    /// </summary>
    private async Task GetCorrelationIdAsync()
    {
        // In the configuration, the variable action name and the variable name should be defined.
        // Firstly, we retrieve the contents of the action using the action name, then we look for the variable containing the correlation id in the action inputs.
        var relativeUri = new Uri($"/subscriptions/{_configuration[StringConstants.SubscriptionId]!}/resourceGroups/{_configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{_configuration[StringConstants.LogicAppName]!}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{_workflowName}/runs/{_workflowRunProperties.Name!}/actions/{_variableActionName}?api-version={_configuration[StringConstants.LogicAppApiVersion]!}", UriKind.Relative);
        var result = await _azureManagementRepository.GetObjectAsync<WorkflowRunDetailsAction>(relativeUri).ConfigureAwait(false);

        if (result?.Properties?.InputsLink != null)
        {
            var wfActionContent = _actionHelper.GetWorkflowRunActionContent(result.Properties?.InputsLink);
            var data = await _actionHelper.GetActionData(wfActionContent).ConfigureAwait(false);
            var match = data?
                .SelectTokens($"$..[?(@.name == '{_correlationIdVariableName}')]")
                .FirstOrDefault();

            _correlationId = match?["value"]?.ToString();
        }
    }

    /// <summary>
    /// Populate simple, synchronous properties on this instance from the API-provided workflow run payload.
    /// </summary>
    private void SetProperties()
    {
        Id = _workflowRunProperties.Id;
        Name = _workflowRunProperties.Name;
        Type = _workflowRunProperties.Type;

        ClientTrackingId = _workflowRunProperties.Properties?.Correlation?.ClientTrackingId;
        EndTime = _workflowRunProperties.Properties?.EndTime;
        StartTime = _workflowRunProperties.Properties?.StartTime;
        Status = _workflowRunProperties.Properties?.Status;
        WaitEndTime = _workflowRunProperties.Properties?.WaitEndTime;
    }

    /// <summary>
    /// Recursively traverse an action and yield the action itself followed by all actions nested under it.
    /// Implemented as an iterator method using <c>yield return</c> so callers can enumerate the flattened sequence of actions lazily.
    /// </summary>
    /// <param name="action">Action to traverse. If null, the sequence is empty.</param>
    /// <returns>A lazy sequence of the action and all nested actions (depth-first).</returns>
    private static IEnumerable<BaseAction> Traverse(BaseAction? action)
    {
        // If the provided action is null there is nothing to traverse.
        if (action is null) yield break;

        // Yield the current action first. This implements a pre-order traversal: the parent is returned before its children.
        // Consumers of this iterator will therefore see the top-level action before any nested actions.
        yield return action;

        // Obtain the immediate children for this action and recursively traverse each child.
        // We intentionally separate obtaining the immediate children (GetChildren) from the recursive traversal so the child-discovery logic remains testable and explicit.
        foreach (var child in GetChildren(action))
        {
            // Recursively traverse the child's subtree and yield each descendant. The nested loop ensures a full depth-first
            // expansion of the action tree without allocating intermediate collections.
            foreach (var nested in Traverse(child))
            {
                yield return nested;
            }
        }
    }

    /// <summary>
    /// Return the immediate child action sequences for the provided action. This method does not recursively traverse the children;
    /// it only exposes the direct child collections so callers can decide how to traverse them. Each case maps a known container action type to the appropriate child sequence.
    /// </summary>
    /// <param name="action">The action for which to retrieve immediate children.</param>
    /// <returns>A sequence of immediate child actions.</returns>
    private static IEnumerable<BaseAction> GetChildren(BaseAction action)
    {
        return action switch
        {
            // Scope exposes its children via the `Actions` list. Return that list directly so callers can enumerate the immediate children.
            ScopeAction scope => scope.Actions,

            // ForEach and Until may include recorded iterations; we return the flattened sequence of actions contained in those iterations
            // because they represent concrete action runs that should be visited when enumerating the workflow's action history.
            ForEachAction forEach => forEach.Repetitions.SelectMany(i => i.Actions),

            UntilAction until => until.Repetitions.SelectMany(i => i.Actions),

            // Switch cases each contain an Actions collection; flatten all cases into a single sequence of case actions so all branches are visible to callers.
            SwitchAction sw => sw.Cases.SelectMany(c => c.Actions),

            // Condition has two separate child lists (ElseActions and DefaultActions); delegate to the helper that yields both in a deterministic order.
            ConditionAction cond => GetConditionChildren(cond),

            // By default, unknown or leaf action types have no children.
            _ => []
        };
    }

    /// <summary>
    /// Yield the immediate children of a ConditionAction. We yield the else-branch actions first and then the default (then) actions.
    /// The ordering is chosen to make the else branch visible before the default branch when enumerating; callers should primarily rely on the overall
    /// depth-first semantics rather than the exact branch order.
    /// </summary>
    /// <param name="cond">The condition action for which to retrieve immediate children.</param>
    /// <returns>A sequence of immediate child actions.</returns>
    private static IEnumerable<BaseAction> GetConditionChildren(ConditionAction cond)
    {
        // Yield explicit else-branch actions (if any).
        foreach (var elseAction in cond.ElseActions)
        {
            yield return elseAction;
        }

        // Then yield default/then actions.
        foreach (var defaultAction in cond.DefaultActions)
        {
            yield return defaultAction;
        }
    }
}