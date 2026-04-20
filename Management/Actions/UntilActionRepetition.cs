using LogicApps.Management.Models.RestApi;

namespace LogicApps.Management.Actions;

public class UntilActionRepetition
{
    public List<BaseAction> Actions { get; set; } = [];

    public List<ActionRepetitionIndex>? RepetitionIndexes { get; private set; }

    public DateTime? StartTime { get; private set; }

    public DateTime? EndTime { get; private set; }

    public string? ActionTrackingId { get; private set; }

    public bool? CanResubmit { get; private set; }

    public string? ClientTrackingId { get; private set; }

    public string? Status { get; private set; }

    public string? Code { get; private set; }

    public string? Id { get; private set; }

    public int? IterationCount { get; private set; }

    public string? Name { get; private set; }

    public string? Type { get; private set; }

    public string? TrackingId { get; private set; }

    public UntilActionRepetition(WorkflowRunDetailsActionRepetition repetition)
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
        TrackingId = repetition.Properties?.TrackingId;
        CanResubmit = repetition.Properties?.CanResubmit;
        IterationCount= repetition.Properties?.IterationCount;


        RepetitionIndexes = [];
        if (repetition.Properties?.RepetitionIndexes == null)
        {
            return;
        }
        
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