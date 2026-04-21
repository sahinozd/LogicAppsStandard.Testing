using LogicApps.Management;
using LogicApps.Management.Actions;

namespace LogicApps.TestFramework.Specifications;

/// <summary>
/// Provides navigation and querying capabilities for workflow run actions, including support for nested structures like loops, scopes, conditions, and switches.
/// </summary>
public class WorkflowRunNavigator(WorkflowRun workflowRun)
{
    private readonly WorkflowRun _workflowRun = workflowRun ?? throw new ArgumentNullException(nameof(workflowRun));

    /// <summary>
    /// Asynchronously retrieves the list of top-level actions for the current workflow run.
    /// </summary>
    /// <remarks>Top-level actions are those that are not contained within any parent action. Use this method
    /// to obtain actions that represent the primary steps of the workflow.</remarks>
    /// <returns>A list of actions that are not nested within other actions. The list is empty if no top-level actions are found.</returns>
    public async Task<IList<BaseAction>> GetTopLevelActionsAsync()
    {
        var allActions = await _workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);
        var nestedActionNames = GetAllNestedActionNames(allActions);

        return [.. allActions.Where(a => !nestedActionNames.Contains(a.Name))];
    }

    /// <summary>
    /// Retrieves the set of action names that are nested within the provided collection of actions.
    /// </summary>
    /// <remarks>The returned set includes names from child actions of supported composite action types, such
    /// as scopes, conditions, switches, and loops. Name comparisons are case-insensitive.</remarks>
    /// <param name="allActions">A list of actions from which to extract the names of all nested child actions. Cannot be null.</param>
    /// <returns>A set of strings containing the names of all nested actions found within the specified actions.
    /// The set is empty if no nested actions are present.</returns>
    private static HashSet<string> GetAllNestedActionNames(List<BaseAction> allActions)
    {
        var nestedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var action in allActions)
        {
            // Collect names from each action type's child collections
            switch (action)
            {
                case ScopeAction scopeAction:
                    foreach (var childAction in scopeAction.Actions)
                    {
                        nestedNames.Add(childAction.Name);
                    }
                    break;

                case ConditionAction conditionAction:
                    foreach (var childAction in conditionAction.DefaultActions.Concat(conditionAction.ElseActions))
                    {
                        nestedNames.Add(childAction.Name);
                    }
                    break;

                case SwitchAction switchAction:
                    foreach (var childAction in switchAction.Cases.SelectMany(c => c.Actions))
                    {
                        nestedNames.Add(childAction.Name);
                    }
                    break;

                case ForEachAction foreachAction:
                    foreach (var childAction in foreachAction.Repetitions.SelectMany(r => r.Actions))
                    {
                        nestedNames.Add(childAction.Name);
                    }
                    break;

                case UntilAction untilAction:
                    foreach (var childAction in untilAction.Repetitions.SelectMany(r => r.Actions))
                    {
                        nestedNames.Add(childAction.Name);
                    }
                    break;
            }
        }

        return nestedNames;
    }

    /// <summary>
    /// Asynchronously retrieves a list of actions that match the specified action name.
    /// </summary>
    /// <param name="actionName">The name of the action to search for. This value is case-sensitive and cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of actions matching the
    /// specified name, or null if no matching actions are found.</returns>
    public async Task<List<BaseAction>?> FindActionAsync(string actionName)
    {
        return await _workflowRun.FindActionByNameAsync(actionName).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously retrieves the list of actions that are direct children of the specified parent action.
    /// </summary>
    /// <param name="parentActionName">The name of the parent action for which to retrieve direct child actions. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of direct child actions of
    /// the specified parent action. Returns an empty list if the parent action is not found or has no direct children.</returns>
    public async Task<IList<BaseAction>> GetChildActionsAsync(string parentActionName)
    {
        var parentActions = await FindActionAsync(parentActionName).ConfigureAwait(false);
        var parentAction = parentActions?.FirstOrDefault();

        if (parentAction == null)
        {
            return Array.Empty<BaseAction>();
        }

        // Get child actions based on parent type
        return parentAction switch
        {
            ScopeAction scopeAction => scopeAction.Actions,
            ForEachAction foreachAction => [.. foreachAction.Repetitions.SelectMany(r => r.Actions)],
            UntilAction untilAction => [.. untilAction.Repetitions.SelectMany(r => r.Actions)],
            ConditionAction conditionAction => [.. conditionAction.DefaultActions, .. conditionAction.ElseActions],
            SwitchAction switchAction => [.. switchAction.Cases.SelectMany(c => c.Actions)],
            _ => Array.Empty<BaseAction>()
        };
    }

    /// <summary>
    /// Retrieves the list of actions executed during a specific iteration of a 'ForEach' loop action.
    /// </summary>
    /// <param name="loopActionName">The name of the loop action to search for. This identifies the 'ForEach' action whose iteration
    /// actions are to be retrieved.</param>
    /// <param name="iterationIndex">The zero-based index of the iteration for which to retrieve actions.</param>
    /// <returns>A list of actions executed in the specified iteration of the loop, or null if the loop or iteration is not found.</returns>
    public async Task<List<BaseAction>?> GetActionsInForEachIterationAsync(string loopActionName, int iterationIndex)
    {
        var loopActions = await FindActionAsync(loopActionName).ConfigureAwait(false);
        var foreachAction = loopActions?.FirstOrDefault() as ForEachAction;

        var repetition = foreachAction?.Repetitions.FirstOrDefault(r => r.RepetitionIndexes?.Any(ri => ri.ItemIndex == iterationIndex) == true);

        return repetition?.Actions;
    }

    /// <summary>
    /// Asynchronously retrieves the list of actions associated with a specific iteration of an 'Until' loop action.
    /// </summary>
    /// <remarks>If the specified loop action does not exist, is not an 'UntilAction', or does not contain the requested iteration, the method returns null.</remarks>
    /// <param name="loopActionName">The name of the loop action to search for. This should correspond to an action of type 'UntilAction'.</param>
    /// <param name="iterationIndex">The zero-based index of the iteration for which to retrieve actions.</param>
    /// <returns>A list of actions executed during the specified iteration of the loop, or null if the loop action or the
    /// iteration is not found.</returns>
    public async Task<List<BaseAction>?> GetActionsInUntilIterationAsync(string loopActionName, int iterationIndex)
    {
        var loopActions = await FindActionAsync(loopActionName).ConfigureAwait(false);
        var untilAction = loopActions?.FirstOrDefault() as UntilAction;

        var repetition = untilAction?.Repetitions.FirstOrDefault(r => r.RepetitionIndexes?.Any(ri => ri.ItemIndex == iterationIndex) == true);

        return repetition?.Actions;
    }

    /// <summary>
    /// Asynchronously retrieves the list of actions that are within the specified scope, optionally filtered by branch name.
    /// </summary>
    /// <remarks>If a branch name is specified, only actions matching the given branch are included in the result.
    /// This is useful for scenarios where actions are organized by conditional branches or switch cases within
    /// a scope. For Conditions, use "actions" for the true branch and "else" for the false branch.</remarks>
    /// <param name="scopeName">The name of the scope from which to retrieve actions. Cannot be null or empty.</param>
    /// <param name="branchName">The name of the branch to filter actions by. For Condition: "actions" (true) or "else" (false). 
    /// For Switch: the case name. If null or empty, all actions in the specified scope are returned.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of actions in the specified
    /// scope, filtered by branch if provided.</returns>
    public async Task<IList<BaseAction>> GetActionsInScopeAsync(string scopeName, string? branchName = null)
    {
        var parentActions = await FindActionAsync(scopeName).ConfigureAwait(false);
        var parentAction = parentActions?.FirstOrDefault();

        if (parentAction == null)
        {
            return Array.Empty<BaseAction>();
        }

        // If no branch specified, return all child actions
        if (string.IsNullOrEmpty(branchName))
        {
            return await GetChildActionsAsync(scopeName).ConfigureAwait(false);
        }

        // Filter by branch based on parent type
        return parentAction switch
        {
            ConditionAction conditionAction => GetConditionBranchActions(conditionAction, branchName),
            SwitchAction switchAction => GetSwitchCaseActions(switchAction, branchName),
            ScopeAction scopeAction => scopeAction.Actions, // Scopes don't have branches
            _ => Array.Empty<BaseAction>()
        };
    }

    /// <summary>
    /// Retrieves the list of actions associated with a specified branch of a conditional action.
    /// </summary>
    /// <param name="conditionAction">The conditional action containing the branches and their associated actions.</param>
    /// <param name="branchName">The name of the branch for which to retrieve actions. Use "actions" for the default branch or "else" for the
    /// else branch. Comparison is case-insensitive.</param>
    /// <returns>A list of actions for the specified branch. Returns an empty list if the branch name does not match a known branch.</returns>
    private static List<BaseAction> GetConditionBranchActions(ConditionAction conditionAction, string branchName)
    {
        return branchName.Equals("actions", StringComparison.OrdinalIgnoreCase)
            ? conditionAction.DefaultActions
            : branchName.Equals("else", StringComparison.OrdinalIgnoreCase)
                ? conditionAction.ElseActions
                : [];
    }

    /// <summary>
    /// Retrieves the list of actions associated with the specified case name from the given switch action.
    /// </summary>
    /// <param name="switchAction">The switch action containing the cases to search. Cannot be null.</param>
    /// <param name="caseName">The name of the case for which to retrieve actions. The comparison is case-insensitive.</param>
    /// <returns>A list of actions associated with the specified case name. Returns an empty list if the case is not found.</returns>
    private static List<BaseAction> GetSwitchCaseActions(SwitchAction switchAction, string caseName)
    {
        var switchCase = switchAction.Cases.FirstOrDefault(c =>
            c.Name.Equals(caseName, StringComparison.OrdinalIgnoreCase));

        return switchCase?.Actions ?? [];
    }

    /// <summary>
    /// Asynchronously retrieves the list of nested loop actions within a specific iteration of a parent 'for-each' loop.
    /// </summary>
    /// <remarks>Only actions that are themselves 'for-each' or 'until' loops are included in the result.
    /// The returned list may be empty if no such nested loops exist in the specified iteration.</remarks>
    /// <param name="parentLoopName">The name of the parent 'for-each' loop whose iteration is being inspected.</param>
    /// <param name="iterationIndex">The zero-based index of the iteration within the parent loop to examine.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of actions representing
    /// nested 'for-each' or 'until' loops found in the specified iteration, or null if no actions are present.</returns>
    public async Task<List<BaseAction>?> GetNestedLoopsInForEachIterationAsync(string parentLoopName, int iterationIndex)
    {
        var actionsInIteration = await GetActionsInForEachIterationAsync(parentLoopName, iterationIndex).ConfigureAwait(false);
        return actionsInIteration?
            .Where(a => a is ForEachAction or UntilAction)
            .ToList();
    }

    /// <summary>
    /// Asynchronously retrieves all nested loop actions within a specified iteration of an 'Until' loop.
    /// </summary>
    /// <param name="parentLoopName">The name of the parent 'Until' loop from which to retrieve nested loop actions.</param>
    /// <param name="iterationIndex">The zero-based index of the iteration within the parent loop to inspect for nested loops.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of nested loop actions of
    /// type 'ForEachAction' or 'UntilAction' found within the specified iteration, or null if no actions are found.</returns>
    public async Task<List<BaseAction>?> GetNestedLoopsInUntilIterationAsync(string parentLoopName, int iterationIndex)
    {
        var actionsInIteration = await GetActionsInUntilIterationAsync(parentLoopName, iterationIndex).ConfigureAwait(false);
        return actionsInIteration?
            .Where(a => a is ForEachAction or UntilAction)
            .ToList();
    }
}