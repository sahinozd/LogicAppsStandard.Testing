namespace LogicApps.Management;

/// <summary>
/// Represents schedule details for a workflow trigger (for example the hours of the day when the trigger should run).
/// </summary>
public class WorkflowTriggerSchedule
{
    public IEnumerable<int>? Hours { get; set; }
}