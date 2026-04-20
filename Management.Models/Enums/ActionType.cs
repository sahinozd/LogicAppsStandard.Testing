namespace LogicApps.Management.Models.Enums;

/// <summary>
/// Enum representing the supported action types used in workflow definitions and runtime metadata.
/// </summary>
public enum ActionType
{
    Unknown,
    Action,
    Scope,
    Until,
    ForEach,
    Switch,
    Condition
}
