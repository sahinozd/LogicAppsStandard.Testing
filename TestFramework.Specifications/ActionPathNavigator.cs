using LogicApps.Management.Actions;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LogicApps.TestFramework.Specifications;

/// <summary>
/// Provides path-based navigation through workflow actions using array-style indexing.
/// Supports paths like "For each number[1].For each letter[2]" or "Until[3].Condition.actions".
/// </summary>
public static partial class ActionPathNavigator
{
    /// <summary>
    /// Navigates to actions at the specified path within the workflow.
    /// </summary>
    /// <param name="allActions">All workflow actions at the top level.</param>
    /// <param name="path">The navigation path, e.g., "For each number[1].For each letter[2]", "Try.Until[3].Condition.actions", or "Switch.Default"</param>
    /// <returns>List of actions at the specified path, or empty list if path is invalid.</returns>
    public static IList<BaseAction> NavigateToPath(IList<BaseAction> allActions, string path)
    {
        ArgumentNullException.ThrowIfNull(allActions);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var segments = ParsePath(path);
        var currentActions = allActions;

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            // Look ahead to see if next segment is a branch name (condition or switch case)
            // For conditions: "actions" or "else"
            // For switches: any segment after a switch action could be a case name
            var nextIsBranch = i + 1 < segments.Count &&
                             (segments[i + 1].ActionName.Equals("actions", StringComparison.OrdinalIgnoreCase) ||
                              segments[i + 1].ActionName.Equals("else", StringComparison.OrdinalIgnoreCase) ||
                              IsPotentiallySwitchCaseSegment(currentActions, segments[i + 1].ActionName));

            currentActions = NavigateSegment(currentActions, segment, nextIsBranch);
            if (currentActions.Count == 0)
            {
                return Array.Empty<BaseAction>();
            }
        }

        return currentActions;
    }

    /// <summary>
    /// Determines whether the specified segment name could represent a case label within the context of a switch action.
    /// </summary>
    /// <remarks>This method returns false for segment names that are known non-case keywords, such as "actions" or "else".
    /// It only returns true if a switch action exists in the current actions list.</remarks>
    /// <param name="currentActions">The list of actions currently in scope, used to determine if a switch action is present.</param>
    /// <param name="segmentName">The name of the segment to evaluate as a potential switch case label. Comparison is case-insensitive.</param>
    /// <returns>true if the segment name could be interpreted as a switch case label in the current context; otherwise, false.</returns>
    private static bool IsPotentiallySwitchCaseSegment(IList<BaseAction> currentActions, string segmentName)
    { 
        return !segmentName.Equals("actions", StringComparison.OrdinalIgnoreCase) &&
               !segmentName.Equals("else", StringComparison.OrdinalIgnoreCase) &&
               currentActions.OfType<SwitchAction>().Any();
    }
        
    /// <summary>
    /// Parses a dot-delimited path string into a list of path segments, extracting segment names and optional indices.
    /// </summary>
    /// <remarks>Segments are split on the '.' character. Leading and trailing whitespace in each segment is trimmed.
    /// If a segment includes an index in square brackets (e.g., "Step[2]"), the index is parsed as an integer; otherwise, the index is null.</remarks>
    /// <param name="path">The dot-delimited path string to parse. Each segment may optionally include an index in square brackets, such as
    /// "Action[1]". Cannot be null or empty.</param>
    /// <returns>A list of path segments representing each part of the parsed path. Segments with indices are represented with
    /// their corresponding index; segments without indices have a null index.</returns>
    private static List<PathSegment> ParsePath(string path)
    {
        var segments = new List<PathSegment>();
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var match = IndexedActionRegex().Match(part);

            if (match.Success)
            {
                // Has index like "For each number[1]"
                var actionName = match.Groups[1].Value;
                var index = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                segments.Add(new PathSegment(actionName, index));
            }
            else
            {
                // No index, like "Try" or "actions" or "else"
                segments.Add(new PathSegment(part, null));
            }
        }

        return segments;
    }

    /// <summary>
    /// Navigates to a specific set of child actions within the provided action list based on the specified path segment.
    /// </summary>
    /// <remarks>Special handling is applied for condition branches such as "actions" and "else", and for switch cases.
    /// If an index is specified in the segment, the method navigates to the corresponding iteration of the action.</remarks>
    /// <param name="currentActions">The list of actions to search within for the target segment.</param>
    /// <param name="segment">The path segment that specifies the action name and, optionally, an index to navigate to.</param>
    /// <param name="nextSegmentIsBranch">Indicates whether the next segment in the path is a branch selector (e.g., "actions" or "else").
    /// When true and the current segment resolves to a ConditionAction, the method returns the condition itself wrapped in a list instead of its children.</param>
    /// <returns>A list of child actions corresponding to the specified segment. Returns an empty list if the segment does not
    /// match any action.</returns>
    private static IList<BaseAction> NavigateSegment(IList<BaseAction> currentActions, PathSegment segment, bool nextSegmentIsBranch = false)
    {
        // Special handling for condition branches
        if (segment.ActionName.Equals("actions", StringComparison.OrdinalIgnoreCase) ||
            segment.ActionName.Equals("else", StringComparison.OrdinalIgnoreCase))
        {
            return NavigateToBranch(currentActions, segment.ActionName);
        }

        // Find the action by name FIRST
        var action = currentActions.FirstOrDefault(a =>
            a.DesignerName?.Equals(segment.ActionName, StringComparison.OrdinalIgnoreCase) == true ||
            a.Name.Equals(segment.ActionName, StringComparison.OrdinalIgnoreCase));

        if (action == null)
        {
            // Action not found by name - check if this might be a switch case name
            var switchAction = currentActions.OfType<SwitchAction>().FirstOrDefault();
            if (switchAction != null)
            {
                // This segment is a switch case name
                return NavigateToSwitchCase(switchAction, segment.ActionName);
            }

            // Not found as action or switch case
            return Array.Empty<BaseAction>();
        }

        // If there's an index, navigate to that iteration
        if (segment.Index.HasValue)
        {
            return NavigateToIteration(action, segment.Index.Value);
        }

        // If next segment is a branch/case and this is a ConditionAction or SwitchAction, return the action itself
        if (nextSegmentIsBranch && (action is ConditionAction || action is SwitchAction))
        {
            return [action];
        }

        // No index, return child actions directly
        return GetChildActions(action);
    }

    /// <summary>
    /// Retrieves the actions associated with a specific iteration of a loop action.
    /// </summary>
    /// <remarks>The method supports loop actions of type ForEachAction and UntilAction. The index parameter is 1-based; the method converts it to 0-based indexing internally.</remarks>
    /// <param name="action">The loop action from which to retrieve iteration actions. Must be a supported loop type such as ForEachAction or UntilAction.</param>
    /// <param name="index">The 1-based index of the iteration for which to retrieve actions. Must be greater than zero.</param>
    /// <returns>A list of actions corresponding to the specified iteration.
    /// Returns an empty list if the action type is not supported or if the index is out of range.</returns>
    private static IList<BaseAction> NavigateToIteration(BaseAction action, int index)
    {
        // Convert 1-based to 0-based
        var zeroBasedIndex = index - 1;

        return action switch
        {
            ForEachAction foreachAction => GetForEachIterationActions(foreachAction, zeroBasedIndex),
            UntilAction untilAction => GetUntilIterationActions(untilAction, zeroBasedIndex),
            _ => Array.Empty<BaseAction>()
        };
    }

    /// <summary>
    /// Retrieves the list of actions to execute for a specific iteration within a 'for each' action, based on the provided index.
    /// </summary>
    /// <remarks>If no repetition matches both the specified index and the scope name, the method attempts to find a repetition matching only the index for backward compatibility.</remarks>
    /// <param name="foreachAction">The 'for each' action containing the collection of repetitions and their associated actions.</param>
    /// <param name="index">The zero-based index of the iteration for which to retrieve actions.</param>
    /// <returns>A list of actions associated with the specified iteration. Returns an empty list if no actions are found for the given index.</returns>
    private static List<BaseAction> GetForEachIterationActions(ForEachAction foreachAction, int index)
    {
        // Find the repetition where ANY RepetitionIndex matches both the index and scope name
        var repetition = foreachAction.Repetitions.FirstOrDefault(repetitions =>
            repetitions.RepetitionIndexes?.Any(actionRepetitionIndex =>
                actionRepetitionIndex.ItemIndex == index && 
                actionRepetitionIndex.ScopeName == foreachAction.Name) == true);

        return repetition?.Actions ?? (List<BaseAction>)[];
    }

    /// <summary>
    /// Retrieves the list of actions associated with a specific iteration of an UntilAction, identified by the given index.
    /// </summary>
    /// <remarks>This method first attempts to match repetitions by both index and scope name to ensure correct nesting.
    /// If no match is found, it falls back to matching by index only for backward compatibility.</remarks>
    /// <param name="untilAction">The UntilAction instance containing the repetitions and associated actions.</param>
    /// <param name="index">The zero-based index of the iteration for which to retrieve actions.</param>
    /// <returns>A list of BaseAction objects for the specified iteration. Returns an empty list if no actions are found for the given index.</returns>
    private static List<BaseAction> GetUntilIterationActions(UntilAction untilAction, int index)
    {
        // Match repetitions by index AND scope name to ensure we're at the right nesting level
        var repetition = untilAction.Repetitions.FirstOrDefault(repetitions =>
            repetitions.RepetitionIndexes?.Any(actionRepetitionIndex =>
                actionRepetitionIndex.ItemIndex == index &&
                actionRepetitionIndex.ScopeName == untilAction.Name) == true);

        return repetition?.Actions ?? (List<BaseAction>)[];
    }

    /// <summary>
    /// Retrieves the list of actions associated with a specified branch of a condition action.
    /// </summary>
    /// <remarks>This method assumes that the current context contains the children of a condition action.
    /// It searches for a condition action within the provided actions to access its branches.</remarks>
    /// <param name="currentActions">The collection of actions representing the current context, expected to include a condition action whose branches are to be accessed.</param>
    /// <param name="branchName">The name of the branch to navigate to. Use "actions" to select the default branch;
    /// any other value selects the else branch. Comparison is case-insensitive.</param>
    /// <returns>A list of actions belonging to the specified branch of the condition action. Returns an empty list if no condition action is found in the current context.</returns>
    private static IList<BaseAction> NavigateToBranch(IList<BaseAction> currentActions, string branchName)
    {
        // The previous segment should have been a Condition action
        // Since we already navigated to it, currentActions contains the condition's children
        // We need to go back and get the actual condition to access its branches

        // This is a special case - we need the parent context
        // For now, look for ConditionAction in current actions
        var conditionAction = currentActions.OfType<ConditionAction>().FirstOrDefault();

        if (conditionAction == null)
        {
            return Array.Empty<BaseAction>();
        }

        return branchName.Equals("actions", StringComparison.OrdinalIgnoreCase)
            ? conditionAction.DefaultActions
            : conditionAction.ElseActions;
    }

    /// <summary>
    /// Retrieves the list of actions associated with a specific case of a switch action.
    /// </summary>
    /// <param name="switchAction">The switch action containing the cases.</param>
    /// <param name="caseName">The name of the case to navigate to. Comparison is case-insensitive.</param>
    /// <returns>A list of actions belonging to the specified case. Returns an empty array if the case is not found.</returns>
    private static BaseAction[] NavigateToSwitchCase(SwitchAction switchAction, string caseName)
    {
        // Find the specific case (case-insensitive comparison)
        var switchCase = switchAction.Cases.FirstOrDefault(c => c.Name.Equals(caseName, StringComparison.OrdinalIgnoreCase));

        return switchCase?.Actions.ToArray() ?? [];
    }

    /// <summary>
    /// Retrieves the collection of child actions contained within the specified action.
    /// </summary>
    /// <remarks>The returned collection may include actions from different branches or repetitions, depending on the type of the provided action.</remarks>
    /// <param name="action">The action from which to retrieve child actions. This parameter must not be null.</param>
    /// <returns>A list of child actions contained within the specified action. Returns an empty list if the action does not contain any child actions.</returns>
    private static IList<BaseAction> GetChildActions(BaseAction action)
    {
        return action switch
        {
            ScopeAction scopeAction => scopeAction.Actions,
            ConditionAction conditionAction => [.. conditionAction.DefaultActions, .. conditionAction.ElseActions],
            SwitchAction switchAction => [.. switchAction.Cases.SelectMany(c => c.Actions)],
            ForEachAction foreachAction => [.. foreachAction.Repetitions.SelectMany(r => r.Actions)],
            UntilAction untilAction => [.. untilAction.Repetitions.SelectMany(r => r.Actions)],
            _ => Array.Empty<BaseAction>()
        };
    }

    [GeneratedRegex(@"^(.+?)\[(\d+)\]$")]
    private static partial Regex IndexedActionRegex();

    /// <summary>
    /// Represents a segment of an action path.
    /// </summary>
    private sealed record PathSegment(string ActionName, int? Index);
}