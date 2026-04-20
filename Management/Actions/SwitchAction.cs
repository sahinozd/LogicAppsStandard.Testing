using LogicApps.Management.Models.Enums;

namespace LogicApps.Management.Actions;

/// <summary>
/// Represents a Switch action that contains multiple named cases and an optional default branch.
/// </summary>
public class SwitchAction(string name, ActionType actionType) : BaseAction(name, actionType)
{
    /// <summary>
    /// The declared cases of the switch statement.
    /// </summary>
    public List<SwitchCase> Cases { get; set; } = [];
}