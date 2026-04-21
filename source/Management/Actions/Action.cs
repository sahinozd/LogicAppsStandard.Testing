using LogicApps.Management.Models.Enums;

namespace LogicApps.Management.Actions;

/// <summary>
/// Represents a simple, regular action in a workflow run that is not a switch, condition, loop, etc. This class is used for leaf actions that do not
/// contain nested child actions.
/// </summary>
public class Action : BaseAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Action"/> class with the specified name and action type.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <param name="actionType">The action type.</param>
    public Action(string name, ActionType actionType) : base(name, actionType)
    {
    }
}