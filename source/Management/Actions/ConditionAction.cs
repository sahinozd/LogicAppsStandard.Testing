using LogicApps.Management.Models.Enums;

namespace LogicApps.Management.Actions;

/// <summary>
/// Represents a Condition (If/Else) action and contains collections for default (then) and else branch actions.
/// </summary>
public class ConditionAction(string name, ActionType actionType) : BaseAction(name, actionType)
{
    public List<BaseAction> ElseActions { get; set; } = [];

    public List<BaseAction> DefaultActions { get; set; } = [];
}