
namespace LogicApps.Management.Actions;

/// <summary>
/// Simple error DTO used to represent action-level errors (code and message).
/// </summary>
public class ActionError
{
    public string? Code { get; set; }

    public string? Message { get; set; }
}