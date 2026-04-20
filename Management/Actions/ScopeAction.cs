using LogicApps.Management.Models.Enums;

namespace LogicApps.Management.Actions;

/// <summary>
/// Represents a Scope action which groups multiple child actions.
/// </summary>
public class ScopeAction(string name, ActionType actionType) : BaseAction(name, actionType)
{
    public List<BaseAction> Actions { get; set; } = [];
}
