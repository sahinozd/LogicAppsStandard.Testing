using LogicApps.Management.Models.RestApi;

namespace LogicApps.Management.Actions;

/// <summary>
/// Represents a single repetition/iteration instance of a ForEach action. Holds repetition metadata and the child actions
/// executed during that iteration.
/// </summary>
public class ForEachActionRepetition
{
    public List<BaseAction> Actions { get; set; } = [];

    public List<ActionRepetitionIndex>? RepetitionIndexes { get; private set; }

    public DateTime? StartTime { get; private set; }

    public DateTime? EndTime { get; private set; }

    public string? ActionTrackingId { get; private set; }

    public string? ClientTrackingId { get; private set; }

    public string? Status { get; private set; }

    public string? Code { get; private set; }

    public string? Id { get; private set; }

    public string? Name { get; private set; }

    public string? Type { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="ForEachActionRepetition"/> from the management API repetition payload.
    /// </summary>
    /// <param name="repetition">API model describing the repetition.</param>
    public ForEachActionRepetition(WorkflowRunDetailsActionRepetition repetition)
    {
        ArgumentNullException.ThrowIfNull(repetition);
        SetProperties(repetition);
    }

    /// <summary>
    /// Map fields from the API repetition payload into this object's properties.
    /// </summary>
    /// <param name="repetition">API repetition payload.</param>
    private void SetProperties(WorkflowRunDetailsActionRepetition repetition)
    {
        Id = repetition.Id;
        Name = repetition.Name;
        Type = repetition.Type;
        Code = repetition.Properties?.Code;
        Status = repetition.Properties?.Status;
        StartTime = repetition.Properties?.StartTime;
        EndTime = repetition.Properties?.EndTime;
        ActionTrackingId = repetition.Properties?.Correlation?.ActionTrackingId;
        ClientTrackingId = repetition.Properties?.Correlation?.ClientTrackingId;

        RepetitionIndexes = [];
        foreach (var repetitionIndex in repetition.Properties?.RepetitionIndexes!)
        {
            var ind = new ActionRepetitionIndex
            {
                ItemIndex = repetitionIndex.ItemIndex,
                ScopeName = repetitionIndex.ScopeName
            };

            RepetitionIndexes.Add(ind);
        }
    }
}