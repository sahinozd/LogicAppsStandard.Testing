namespace LogicApps.Management.Actions;

/// <summary>
/// Represents a link to action content (inputs/outputs) including metadata and a Uri to fetch the payload.
/// </summary>
public class ActionContent
{
    public int? ContentSize { get; set; }

    public ActionContentMetadata? Metadata { get; set; }

    public Uri? Uri { get; set; }
}