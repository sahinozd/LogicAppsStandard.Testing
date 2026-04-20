namespace LogicApps.Management.Actions;

/// <summary>
/// Metadata about an action content payload (for example, the number of items in a ForEach payload).
/// </summary>
public class ActionContentMetadata
{
    public int? ForeachItemsCount { get; set; }
}