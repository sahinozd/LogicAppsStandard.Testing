namespace LogicApps.Management.Actions;

/// <summary>
/// Represents a Switch action cases that contains a list of actions in it.
/// </summary>
public class SwitchCase
{
    public required string Name { get; set; }

    public List<BaseAction> Actions { get; set; } = [];
}