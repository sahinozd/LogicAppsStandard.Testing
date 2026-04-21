namespace LogicApps.Management;

/// <summary>
/// Represents the recurrence configuration for a workflow trigger, including frequency, interval, schedule and timezone.
/// This maps to the trigger recurrence properties returned by the management API and is used to calculate or display the trigger scheduling information.
/// </summary>
public class WorkflowTriggerRecurrence
{
    public string? Frequency { get; set; }

    public int? Interval { get; set; }

    public WorkflowTriggerSchedule? Schedule { get; set; }

    public string? TimeZone { get; set; }
}