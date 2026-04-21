using LogicApps.Management.Actions;
using LogicApps.TestFramework.Specifications.Models;

namespace LogicApps.TestFramework.Specifications;

public class WorkflowRunValidation(Management.WorkflowRun workflowRun)
{
    private readonly Management.WorkflowRun _workflowRun = workflowRun ?? throw new ArgumentNullException(nameof(workflowRun));
    private readonly WorkflowRunNavigator _navigator = new(workflowRun);

    #region Legacy/Existing Validation Methods (Backward Compatibility)

    public async Task<(bool, string?)> ValidateRunActionsAsync(IList<WorkflowEvent>? expectedEvents = null)
    {
        var trigger = await _workflowRun.GetWorkflowRunTriggerAsync().ConfigureAwait(false);
        var actions = await _workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);

        if (actions.Count == 0 && trigger == null)
        {
            return (false, "No run actions or triggers were found");
        }

        if (expectedEvents == null)
        {
            return (true, null);
        }

        foreach (var expectedEvent in expectedEvents)
        {
            ValueTuple<bool, string?> result;

            if (trigger != null && trigger.DesignerName == expectedEvent.StepName)
            {
                result = CheckActionStatus(expectedEvent, trigger.Status!, false);
            }
            else
            {
                var foundActions = await _navigator.FindActionAsync(expectedEvent.StepName).ConfigureAwait(false);
                var action = foundActions?.FirstOrDefault();

                result = action != null
                    ? CheckActionStatus(expectedEvent, action.Status!, true)
                    : (false, $"Action/Trigger \"{expectedEvent.StepName}\" was not found");
            }

            if (!result.Item1)
            {
                return result;
            }
        }

        return (true, null);
    }

    public async Task<(bool, string?)> ValidateRunLoopActionsExecutionsAsync(string foreachLoopName, int index, IList<WorkflowEvent>? expectedEvents)
    {
        var actions = await _workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);
        var loopActions = actions.Where(action => action.DesignerName == foreachLoopName).ToList();

        if (loopActions.Count == 0)
        {
            return (false, "Loop action was not found");
        }

        if (expectedEvents == null)
        {
            return (true, null);
        }

        var loopNumber = 1;
        foreach (var loopAction in loopActions)
        {
            var loopPrefix = BuildLoopPrefix(loopActions.Count, loopNumber, foreachLoopName);
            var repetitionActions = GetRepetitionActionsByIndex(loopAction, index, loopPrefix);

            if (repetitionActions.ValidationResult != null)
            {
                return repetitionActions.ValidationResult.Value;
            }

            var result = ValidateExpectedEventsInActions(repetitionActions.Actions!, expectedEvents, loopPrefix);
            if (!result.Item1)
            {
                return result;
            }

            loopNumber++;
        }

        return (true, null);
    }

    #endregion

    #region Simple Validation Methods

    /// <summary>
    /// Validates whether a specified workflow action or trigger has the expected status.
    /// </summary>
    /// <param name="actionName">The name of the workflow action or trigger to validate.</param>
    /// <param name="expectedStatus">The status value that the action or trigger is expected to have.</param>
    /// <returns>A tuple containing a Boolean value that is <see langword="true"/> if the action or trigger has the expected
    /// status; otherwise, <see langword="false"/>. The second item is an optional error message if validation fails; otherwise, <see langword="null"/>.</returns>
    public async Task<(bool, string?)> ValidateSingleActionAsync(string actionName, string expectedStatus)
    {
        var actions = await _navigator.FindActionAsync(actionName).ConfigureAwait(false);
        var action = actions?.FirstOrDefault();

        if (action == null)
        {
            var trigger = await _workflowRun.GetWorkflowRunTriggerAsync().ConfigureAwait(false);
            if (trigger?.DesignerName == actionName)
            {
                var triggerEvent = new WorkflowEvent(actionName, expectedStatus);
                return CheckActionStatus(triggerEvent, trigger.Status!, false);
            }

            return (false, $"Action \"{actionName}\" was not found");
        }

        var expectedEvent = new WorkflowEvent(actionName, expectedStatus);
        return CheckActionStatus(expectedEvent, action.Status!, true);
    }

    /// <summary>
    /// Validates that all expected workflow events correspond to child actions within the specified parent action and branch.
    /// </summary>
    /// <remarks>If any expected event does not match a child action or its status, validation fails and an error message is provided.
    /// If no child actions are found, validation also fails.</remarks>
    /// <param name="parentActionName">The name of the parent action whose child actions are to be validated.</param>
    /// <param name="expectedEvents">A list of expected workflow events, each specifying a child action and its expected status. Cannot be null.</param>
    /// <param name="branch">The name of the branch to search for child actions, or null to use the default branch.</param>
    /// <returns>A tuple where the first value indicates whether all expected events are valid for the child actions, and the second value contains an error message if validation fails; otherwise, null.</returns>
    public async Task<(bool, string?)> ValidateChildActionsAsync(string parentActionName, IList<WorkflowEvent> expectedEvents, string? branch = null)
    {
        ArgumentNullException.ThrowIfNull(expectedEvents);

        var childActions = await _navigator.GetActionsInScopeAsync(parentActionName, branch).ConfigureAwait(false);

        if (childActions.Count == 0)
        {
            return (false, $"No child actions found in \"{parentActionName}\"" + (branch != null ? $" for branch \"{branch}\"" : string.Empty));
        }

        return ValidateExpectedEventsInActions(childActions, expectedEvents, parentActionName);
    }

    #endregion

    #region Loop Validation Methods

    /// <summary>
    /// Validates that all loops with the specified name have executed the expected number of iterations and that each iteration has the expected status.
    /// </summary>
    /// <remarks>This method supports validation for both ForEach and Until loop actions. If multiple loops with the same name exist at different nesting levels, all of them will be validated.
    /// If any loop is not found, is not a supported loop type, does not have the expected iteration count,
    /// or any iteration does not match the expected status, validation fails and an appropriate error message is returned.</remarks>
    /// <param name="loopName">The name of the loop(s) to validate.</param>
    /// <param name="expectedIterations">The expected number of iterations each matching loop should have executed.</param>
    /// <param name="expectedStatus">The expected status that each iteration of each matching loop should have.</param>
    /// <returns>A tuple where the first value indicates whether the validation succeeded for all matching loops,
    /// and the second value contains an error message if validation failed; otherwise, null.</returns>
    public async Task<(bool, string?)> ValidateLoopIterationCountAsync(string loopName, int expectedIterations, string expectedStatus)
    {
        var loopActions = await _navigator.FindActionAsync(loopName).ConfigureAwait(false);

        if (loopActions == null || loopActions.Count == 0)
        {
            return (false, $"Loop \"{loopName}\" was not found");
        }

        var expectedEvent = new WorkflowEvent(loopName, expectedStatus);
        var loopNumber = 1;

        foreach (var loopAction in loopActions)
        {
            var loopPrefix = BuildLoopPrefix(loopActions.Count, loopNumber, loopName);
            var result = ValidateLoopRepetitionCount(loopAction, expectedIterations, expectedEvent, loopPrefix);

            if (!result.Item1)
            {
                return result;
            }

            loopNumber++;
        }

        return (true, null);
    }

    /// <summary>
    /// Validates that all expected workflow actions are present and have the correct status in a specific iteration of a loop.
    /// </summary>
    /// <remarks>This method checks both 'ForEach' and 'Until' loop types to locate the specified iteration.
    /// If any expected action is missing or has an unexpected status, validation fails and an error message is provided.</remarks>
    /// <param name="loopName">The name of the loop containing the iteration to validate.</param>
    /// <param name="iteration">The one-based index of the iteration within the loop to validate.</param>
    /// <param name="expectedEvents">A list of expected workflow events representing the actions and their expected statuses for the specified /// iteration. Cannot be null.</param>
    /// <returns>A tuple where the first value indicates whether all expected actions are present and valid in the specified
    /// iteration, and the second value contains an error message if validation fails; otherwise, null.</returns>
    public async Task<(bool, string?)> ValidateActionsInIterationAsync(string loopName, int iteration, IList<WorkflowEvent> expectedEvents)
    {
        ArgumentNullException.ThrowIfNull(expectedEvents);

        var index = iteration - 1;
        var actionsInIteration =
            await _navigator.GetActionsInForEachIterationAsync(loopName, index).ConfigureAwait(false) ??
            await _navigator.GetActionsInUntilIterationAsync(loopName, index).ConfigureAwait(false);

        if (actionsInIteration == null || actionsInIteration.Count == 0)
        {
            return (false, $"Iteration {iteration} not found in loop \"{loopName}\"");
        }

        foreach (var expectedEvent in expectedEvents)
        {
            var action = actionsInIteration.FirstOrDefault(a => a.DesignerName == expectedEvent.StepName || a.Name == expectedEvent.StepName);

            if (action == null)
            {
                return (false, $"Action \"{expectedEvent.StepName}\" not found in iteration {iteration} of loop \"{loopName}\"");
            }

            var result = CheckActionStatus(expectedEvent, action.Status!, false);
            if (!result.Item1)
            {
                return result;
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Validates that the specified actions occur in every iteration of all loops matching the given name.
    /// </summary>
    /// <remarks>If multiple loops with the same name exist at different nesting levels, all of them will be validated.
    /// If any loop has no iterations or any iteration fails validation, the method returns false with an appropriate error message.
    /// This optimized version retrieves all repetitions once instead of querying the loop action for each iteration separately.</remarks>
    /// <param name="loopName">The name of the loop(s) whose iterations will be validated.</param>
    /// <param name="expectedEvents">A list of expected workflow events that should occur in each iteration of the loop.</param>
    /// <returns>A tuple where the first value indicates whether all iterations of all matching loops are valid, and the second value contains an error
    /// message if validation fails; otherwise, null.</returns>
    public async Task<(bool, string?)> ValidateActionsInAllIterationsAsync(string loopName, IList<WorkflowEvent> expectedEvents)
    {
        ArgumentNullException.ThrowIfNull(expectedEvents);

        var loopActions = await _navigator.FindActionAsync(loopName).ConfigureAwait(false);

        if (loopActions == null || loopActions.Count == 0)
        {
            return (false, $"Loop \"{loopName}\" was not found");
        }

        var loopNumber = 1;
        foreach (var loopAction in loopActions)
        {
            var loopPrefix = BuildLoopPrefix(loopActions.Count, loopNumber, loopName);
            var result = ValidateAllRepetitions(loopAction, expectedEvents, loopPrefix, loopName);

            if (!result.Item1)
            {
                return result;
            }

            loopNumber++;
        }

        return (true, null);
    }

    /// <summary>
    /// Validates that a specified nested loop within a parent loop iteration has executed the expected number of times and that each iteration has the expected status.
    /// </summary>
    /// <remarks>If the parent loop iteration or nested loop is not found, or if the nested loop does not match the expected type or criteria, the method returns false with a descriptive error message.
    /// This method supports both ForEach and Until loop types.</remarks>
    /// <param name="parentLoopName">The name of the parent loop containing the nested loop to validate.</param>
    /// <param name="parentIteration">The one-based index of the parent loop iteration in which to search for the nested loop.</param>
    /// <param name="nestedLoopName">The name of the nested loop to validate within the specified parent iteration.</param>
    /// <param name="expectedIterations">The expected number of times the nested loop should have executed.</param>
    /// <param name="expectedStatus">The expected status that each iteration of the nested loop should have.</param>
    /// <returns>A tuple where the first value indicates whether the nested loop meets the expected criteria, and the second value contains an error message if validation fails; otherwise, null.</returns>
    public async Task<(bool, string?)> ValidateNestedLoopAsync(string parentLoopName, int parentIteration, string nestedLoopName, int expectedIterations, string expectedStatus)
    {
        var parentIndex = parentIteration - 1;

        var nestedLoops =
            await _navigator.GetNestedLoopsInForEachIterationAsync(parentLoopName, parentIndex).ConfigureAwait(false) ??
            await _navigator.GetNestedLoopsInUntilIterationAsync(parentLoopName, parentIndex).ConfigureAwait(false);

        if (nestedLoops == null || nestedLoops.Count == 0)
        {
            return (false, $"No nested loops found in iteration {parentIteration} of \"{parentLoopName}\"");
        }

        var nestedLoop = nestedLoops.FirstOrDefault(l => l.DesignerName == nestedLoopName || l.Name == nestedLoopName);

        if (nestedLoop == null)
        {
            return (false, $"Nested loop \"{nestedLoopName}\" not found in iteration {parentIteration} of \"{parentLoopName}\"");
        }

        var expectedEvent = new WorkflowEvent(nestedLoopName, expectedStatus);
        return ValidateLoopRepetitionCount(nestedLoop, expectedIterations, expectedEvent, string.Empty);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Builds a formatted prefix string for a loop, including the loop number and name if multiple loops are present.
    /// </summary>
    /// <param name="totalLoops">The total number of loops. If greater than 1, the prefix will include loop information.</param>
    /// <param name="loopNumber">The current loop's number to include in the prefix. Used only if there are multiple loops.</param>
    /// <param name="loopName">The name of the loop to include in the prefix. Used only if there are multiple loops.</param>
    /// <returns>A formatted string containing the loop number and name if there are multiple loops; otherwise, an empty string.</returns>
    private static string BuildLoopPrefix(int totalLoops, int loopNumber, string loopName)
    {
        return totalLoops > 1 ? $"Loop #{loopNumber} \"{loopName}\": " : "";
    }

    /// <summary>
    /// Validates that a loop action has executed the expected number of iterations and that each repetition has the expected status.
    /// </summary>
    /// <remarks>If the number of repetitions does not match the expected count, or if any repetition does not have the expected status,
    /// the method returns false and provides a descriptive error message. This method is intended for use in workflow validation scenarios.</remarks>
    /// <param name="loopAction">The loop action to validate. Must represent a supported loop type such as ForEach or Until.</param>
    /// <param name="expectedIterations">The number of iterations that the loop is expected to execute.</param>
    /// <param name="expectedEvent">The workflow event containing the expected step name and status for each loop repetition.</param>
    /// <param name="loopPrefix">A string prefix to include in any error messages for context or identification.</param>
    /// <returns>A tuple where the first value indicates whether the loop repetitions are valid, and the second value contains an
    /// error message if validation fails; otherwise, null.</returns>
    private static (bool, string?) ValidateLoopRepetitionCount(BaseAction loopAction, int expectedIterations, WorkflowEvent expectedEvent, string loopPrefix)
    {
        var repetitions = GetLoopRepetitions(loopAction);

        if (repetitions.Count != expectedIterations)
        {
            return (false, $"{loopPrefix}Loop \"{expectedEvent.StepName}\" ran {repetitions.Count} times, but {expectedIterations} was expected");
        }

        if (repetitions.Count == 0)
        {
            var loopType = loopAction is ForEachAction ? "ForEach" : "Until";
            return (false, $"{loopPrefix}{loopType} loop \"{expectedEvent.StepName}\" has no repetitions");
        }

        foreach (var repetition in repetitions)
        {
            var result = CheckActionStatus(expectedEvent, repetition.Status!, false);
            if (!result.Item1)
            {
                return (false, $"{loopPrefix}{result.Item2}");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Validates that all repetitions of a loop action contain the expected workflow events.
    /// </summary>
    /// <remarks>If the loop contains no repetitions, validation fails and an appropriate error message is
    /// returned. The method checks each repetition in order and returns upon the first validation failure.</remarks>
    /// <param name="loopAction">The loop action to validate, representing the repeated workflow steps.</param>
    /// <param name="expectedEvents">A list of workflow events that are expected to occur in each repetition.</param>
    /// <param name="loopPrefix">A string prefix used to identify the loop in error messages.</param>
    /// <param name="loopName">The name of the loop, used for reporting validation results.</param>
    /// <returns>A tuple where the first value indicates whether all repetitions are valid, and the second value contains an
    /// error message if validation fails; otherwise, null.</returns>
    private static (bool, string?) ValidateAllRepetitions(BaseAction loopAction, IList<WorkflowEvent> expectedEvents, string loopPrefix, string loopName)
    {
        var repetitions = GetLoopRepetitions(loopAction);

        if (repetitions.Count == 0)
        {
            return (false, $"{loopPrefix}Loop \"{loopName}\" has no iterations");
        }

        for (var i = 0; i < repetitions.Count; i++)
        {
            var iterationResult = ValidateActionsInList(repetitions[i].Actions, expectedEvents, i + 1);
            if (!iterationResult.Item1)
            {
                return (false, $"{loopPrefix}Iteration {i + 1}: {iterationResult.Item2}");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Retrieves the list of repetitions for the specified loop action, including their statuses and associated actions.
    /// </summary>
    /// <remarks>This method supports loop actions that provide repetition details, such as ForEachAction and UntilAction.
    /// If an unsupported action type is provided, the result will be an empty list.</remarks>
    /// <param name="loopAction">The loop action from which to extract repetition information. Must be an instance of a supported loop action
    /// type, such as ForEachAction or UntilAction.</param>
    /// <returns>A list of tuples, each containing the status and the list of actions for each repetition of the loop.
    /// Returns an empty list if the loop action type is not supported.</returns>
    private static List<(string? Status, List<BaseAction> Actions)> GetLoopRepetitions(BaseAction loopAction) =>
        loopAction switch
        {
            ForEachAction foreachAction => foreachAction.Repetitions.Select(r => (r.Status, r.Actions)).ToList(),
            UntilAction untilAction => untilAction.Repetitions.Select(r => (r.Status, r.Actions)).ToList(),
            _ => []
        };

    /// <summary>
    /// Retrieves the list of actions associated with a specific repetition index from a loop action.
    /// </summary>
    /// <remarks>If the specified action is not a supported loop type or does not contain the requested repetition,
    /// the method returns null for the actions and provides a validation result describing the issue.</remarks>
    /// <param name="loopAction">The loop action from which to retrieve repetition actions. Must be a ForEachAction or UntilAction.</param>
    /// <param name="index">The zero-based index of the repetition whose actions are to be retrieved.</param>
    /// <param name="loopPrefix">A string prefix to include in validation messages for context or identification.</param>
    /// <returns>A tuple containing the list of actions for the specified repetition index, or null if not found,
    /// and a validation result indicating success or failure with an optional message.</returns>
    private static (List<BaseAction>? Actions, (bool, string?)? ValidationResult) GetRepetitionActionsByIndex(BaseAction loopAction, int index, string loopPrefix)
    {
        var loopName = loopAction.DesignerName ?? loopAction.Name;

        // Get repetitions collection and type name
        var (repetitions, loopType) = loopAction switch
        {
            ForEachAction forEach => (forEach.Repetitions.Cast<object>().ToList(), "ForEach"),
            UntilAction until => (until.Repetitions.Cast<object>().ToList(), "Until"),
            _ => (null, null)
        };

        // Validate loop type
        if (repetitions == null || loopType == null)
        {
            return (null, (false, $"{loopPrefix}Action \"{loopName}\" is not a loop (ForEach or Until)"));
        }

        // Check for empty repetitions
        if (repetitions.Count == 0)
        {
            return (null, (false, $"{loopPrefix}{loopType} loop \"{loopName}\" has no repetitions"));
        }

        // Find matching repetition by index (works for both ForEach and Until)
        var actions = loopAction switch
        {
            ForEachAction foreachAction => foreachAction.Repetitions.FirstOrDefault(r => r.RepetitionIndexes?.Any(ri => ri.ItemIndex == index) == true)?.Actions,
            UntilAction untilAction => untilAction.Repetitions.FirstOrDefault(r => r.RepetitionIndexes?.Any(ri => ri.ItemIndex == index) == true)?.Actions,
            _ => null
        };

        // Validate repetition was found
        if (actions == null)
        {
            return (null, (false, $"{loopPrefix}Loop \"{loopName}\" doesn't contain a repetition with index {index}"));
        }

        return (actions, null);
    }

    /// <summary>
    /// Validates that each expected workflow event corresponds to an action in the provided list and that the action's status matches the expected event.
    /// </summary>
    /// <param name="actions">The list of actions to be checked for matching workflow events.</param>
    /// <param name="expectedEvents">The collection of expected workflow events to validate against the actions.</param>
    /// <param name="context">A string representing the context in which the validation is performed, used for error reporting.</param>
    /// <returns>A tuple where the first value indicates whether all expected events are present and valid in the actions list,
    /// and the second value contains an error message if validation fails; otherwise, null.</returns>
    private static (bool, string?) ValidateExpectedEventsInActions(IList<BaseAction> actions, IList<WorkflowEvent> expectedEvents, string context)
    {
        foreach (var expectedEvent in expectedEvents)
        {
            var action = actions.FirstOrDefault(a => a.DesignerName == expectedEvent.StepName || a.Name == expectedEvent.StepName);

            if (action == null)
            {
                return (false, $"Action \"{expectedEvent.StepName}\" was not found in \"{context}\"");
            }

            var result = CheckActionStatus(expectedEvent, action.Status!, true);
            if (!result.Item1)
            {
                return result;
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Validates that each expected workflow event has a corresponding action in the list with a matching status.
    /// </summary>
    /// <remarks>The method compares each expected event to the provided actions by matching the step name to
    /// either the action's designer name or name, and then checks for a case-insensitive status match.
    /// If any expected event is not found or has a mismatched status, validation fails and an error message is returned.</remarks>
    /// <param name="actions">The list of actions to validate against the expected workflow events. Each action is checked for a matching name and status.</param>
    /// <param name="expectedEvents">The collection of expected workflow events, each specifying a step name and the expected status to be matched.</param>
    /// <param name="iterationNumber">The current iteration number, used for error reporting if validation fails.</param>
    /// <returns>A tuple where the first value indicates whether all expected events are matched by actions with the correct
    /// status, and the second value contains an error message if validation fails; otherwise, null.</returns>
    private static (bool, string?) ValidateActionsInList(List<BaseAction> actions, IList<WorkflowEvent> expectedEvents, int iterationNumber)
    {
        foreach (var expectedEvent in expectedEvents)
        {
            var action = actions.FirstOrDefault(a => a.DesignerName == expectedEvent.StepName || a.Name == expectedEvent.StepName);

            if (action == null)
            {
                return (false, $"Action \"{expectedEvent.StepName}\" not found in iteration {iterationNumber}");
            }

            if (!action.Status!.Equals(expectedEvent.Status, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Action \"{expectedEvent.StepName}\" has status \"{action.Status}\" but expected \"{expectedEvent.Status}\"");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Determines whether the specified workflow event's status matches the expected status and provides an explanatory message if it does not.
    /// </summary>
    /// <param name="expectedEvent">The workflow event containing the expected status and step name to compare against.</param>
    /// <param name="status">The current status to be checked against the expected event status. Comparison is case-insensitive.</param>
    /// <param name="isAction">true if the status check is for an action; false if it is for a trigger. Used to format the explanatory message.</param>
    /// <returns>A tuple where the first value is true if the status matches the expected status; otherwise, false.
    /// The second value is a message describing the mismatch, or null if the status matches.</returns>
    private static (bool, string?) CheckActionStatus(WorkflowEvent expectedEvent, string status, bool isAction)
    {
        return !status.Equals(expectedEvent.Status, StringComparison.OrdinalIgnoreCase)
            ? (false, $"{(isAction ? "Action" : "Trigger")} \"{expectedEvent.StepName}\" has status \"{status}\", while \"{expectedEvent.Status}\" was expected")
            : (true, null);
    }

    #endregion
}