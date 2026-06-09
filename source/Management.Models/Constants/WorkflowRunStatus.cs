namespace LogicApps.Management.Models.Constants;

/// <summary>
/// String constants representing the possible status values for a workflow run,
/// as returned by the Azure Management API.
/// </summary>
public static class WorkflowRunStatus
{
    /// <summary>All actions in the workflow completed successfully.</summary>
    public const string Succeeded = "Succeeded";

    /// <summary>At least one action failed, and subsequent actions were not configured to handle the failure.</summary>
    public const string Failed = "Failed";

    /// <summary>The workflow is currently in progress, or the run is throttled due to action limits.</summary>
    public const string Running = "Running";

    /// <summary>The workflow was triggered but subsequently cancelled, either manually or via a timeout.</summary>
    public const string Cancelled = "Cancelled";

    /// <summary>The run is paused or has not started yet, for instance waiting for a callback or an earlier instance to finish.</summary>
    public const string Waiting = "Waiting";

    /// <summary>The trigger condition was checked but not met, so the run never initiated.</summary>
    public const string Skipped = "Skipped";

    /// <summary>The run exceeded the maximum allowed duration.</summary>
    public const string TimedOut = "Timed out";

    /// <summary>The run did not finish due to external issues, such as a system outage or subscription issue.</summary>
    public const string Aborted = "Aborted";
}
