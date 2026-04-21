using LogicApps.Management.Helper;
using LogicApps.Management.Models.Constants;
using LogicApps.Management.Models.Enums;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace LogicApps.Management.Actions;

/// <summary>
/// Base model for actions in a workflow run. Concrete action types derive from this class and inherit common runtime properties
/// such as timing, status, input/output and repetition counts. This type also provides helper methods to load details from the
/// management API.
/// </summary>
public abstract class BaseAction
{
    public string Name { get; set; }

    public ActionType Type { get; set; }

    public bool? CanResubmit { get; set; }

    public string? Code { get; set; }

    public Correlation? Correlation { get; set; }

    public string? DesignerName { get; set; }

    public DateTime? EndTime { get; set; }

    public ActionError? Error { get; set; }

    public string? Id { get; set; }

    public JToken? Input { get; set; }

    public ActionContent? InputsLink { get; set; }

    public string? OriginHistoryName { get; set; }

    public JToken? Output { get; set; }

    public ActionContent? OutputsLink { get; set; }

    public DateTime? ScheduledTime { get; set; }

    public DateTime? StartTime { get; set; }

    public string? Status { get; set; }

    public string? TrackingId { get; set; }

    public int? IterationCount { get; set; }

    public int? RepetitionCount { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="BaseAction"/> with the specified name and action type.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <param name="actionType">The <see cref="ActionType"/> for this action.</param>
    protected BaseAction(string name, ActionType actionType)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (!Enum.IsDefined(actionType))
        {
            throw new ArgumentOutOfRangeException(nameof(actionType), actionType, "Invalid ActionType value.");
        }

        Name = name;
        Type = actionType;
    }

    /// <summary>
    /// Load full action details for this action from the management API and populate runtime properties (inputs, outputs, timings, error, etc.).
    /// </summary>
    /// <param name="configuration">Configuration containing resource identifiers.</param>
    /// <param name="azureManagementRepository">Repository used to call management endpoints.</param>
    /// <param name="actionHelper">Helper used to interpret content links returned by the API.</param>
    /// <param name="workflowName">Workflow name for the run.</param>
    /// <param name="runId">Workflow run identifier.</param>
    public virtual async Task LoadActionDetails(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionHelper actionHelper, string workflowName, string runId)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(azureManagementRepository);
        ArgumentNullException.ThrowIfNull(actionHelper);
        ArgumentException.ThrowIfNullOrEmpty(workflowName);
        ArgumentException.ThrowIfNullOrEmpty(runId);

        var relativeUri = new Uri($"/subscriptions/{configuration[StringConstants.SubscriptionId]!}/resourceGroups/{configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{configuration[StringConstants.LogicAppName]!}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{workflowName}/runs/{runId}/actions/{Name}?api-version={configuration[StringConstants.LogicAppApiVersion]!}", UriKind.Relative);
        var result = await azureManagementRepository.GetObjectAsync<WorkflowRunDetailsAction>(relativeUri).ConfigureAwait(false);

        if (result != null)
        {
            await SetProperties(actionHelper, result).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Populate this action's properties from an already retrieved <see cref="WorkflowRunDetailsAction"/> instance.
    /// This avoids an extra network call when the caller already has the action data.
    /// </summary>
    /// <param name="actionHelper">Helper used to interpret content links.</param>
    /// <param name="result">The action result payload from the management API.</param>
    public async Task SetActionDetails(IActionHelper actionHelper, WorkflowRunDetailsAction result)
    {
        ArgumentNullException.ThrowIfNull(actionHelper);
        ArgumentNullException.ThrowIfNull(result);

        await SetProperties(actionHelper, result).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieve all historical repetitions for this action from the management API. Follows paging (nextLink) until complete.
    /// </summary>
    /// <param name="configuration">Configuration with subscription/resource identifiers.</param>
    /// <param name="azureManagementRepository">Repository used to call the management API.</param>
    /// <param name="actionHelper">Action helper (not used directly but validated).</param>
    /// <param name="workflowName">Workflow name.</param>
    /// <param name="runId">Workflow run id.</param>
    /// <returns>List of <see cref="WorkflowRunDetailsAction"/> representing each repetition.</returns>
    public virtual async Task<List<WorkflowRunDetailsAction>> GetAllActionRepetitions(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionHelper actionHelper, string workflowName, string runId)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(azureManagementRepository);
        ArgumentNullException.ThrowIfNull(actionHelper);
        ArgumentException.ThrowIfNullOrEmpty(workflowName);
        ArgumentException.ThrowIfNullOrEmpty(runId);

        var repetitions = new List<WorkflowRunDetailsAction>();
        var uriString = $"/subscriptions/{configuration[StringConstants.SubscriptionId]!}/resourceGroups/{configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{configuration[StringConstants.LogicAppName]!}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{workflowName}/runs/{runId}/actions/{Name}/repetitions?api-version={configuration[StringConstants.LogicAppApiVersion]!}";

        // Get action operation returns 250 records by default. When there are more actions, it will give a nextLink, which we use to get the next page of actions. Repeat till no nextLink is left.
        while (!string.IsNullOrEmpty(uriString))
        {
            var relativeUri = new Uri(uriString, UriKind.Relative);
            var result = await azureManagementRepository.GetObjectAsync<Response<WorkflowRunDetailsAction>>(relativeUri).ConfigureAwait(false);

            if (result!.Value != null)
            {
                repetitions.AddRange(result.Value);
            }

            uriString = !string.IsNullOrEmpty(result.NextLink) ? new Uri(result.NextLink).PathAndQuery : string.Empty;
        }

        return repetitions;
    }

    /// <summary>
    /// Internal helper to map API response fields into the BaseAction properties (inputs, outputs, timing, error, correlation, etc.).
    /// </summary>
    /// <param name="actionHelper">Helper used to fetch linked action content.</param>
    /// <param name="result">API response containing action details.</param>
    private async Task SetProperties(IActionHelper actionHelper, WorkflowRunDetailsAction result)
    {
        var props = result.Properties;

        DesignerName = Name.Replace("_", " ", StringComparison.OrdinalIgnoreCase);
        EndTime = props?.EndTime;
        Id = result.Id;
        CanResubmit = props?.CanResubmit;
        Code = props?.Code;
        OriginHistoryName = props?.OriginHistoryName;
        ScheduledTime = props?.ScheduledTime;
        Status = props?.Status;
        StartTime = props?.StartTime;
        IterationCount = props?.IterationCount;
        RepetitionCount = props?.RepetitionCount;
        TrackingId = props?.TrackingId;

        Correlation = MapCorrelation(props);
        Error = MapError(props);

        (InputsLink, Input) = await BuildContentAsync(actionHelper, props?.InputsLink).ConfigureAwait(false);
        (OutputsLink, Output) = await BuildContentAsync(actionHelper, props?.OutputsLink).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps the correlation information from the specified workflow run details properties to a new Correlation instance.
    /// </summary>
    /// <param name="properties">The workflow run details properties containing correlation information to map. Can be null.</param>
    /// <returns>A new Correlation instance with values mapped from the specified properties; or null if the correlation information is not present.</returns>
    private static Correlation? MapCorrelation(WorkflowRunDetailsActionProperties? properties)
    {
        var correlation = properties?.Correlation;
        if (correlation == null) return null;

        return new Correlation
        {
            ClientTrackingId = correlation.ClientTrackingId,
            ActionTrackingId = correlation.ActionTrackingId
        };
    }

    /// <summary>
    /// Maps the error information from the specified action properties to an ActionError instance.
    /// </summary>
    /// <param name="properties">The action properties containing error details to map. Can be null.</param>
    /// <returns>An ActionError instance containing the mapped error details if an error is present; otherwise, null.</returns>
    private static ActionError? MapError(WorkflowRunDetailsActionProperties? properties)
    {
        var error = properties?.Error;
        if (error == null) return null;

        return new ActionError
        {
            Code = error.Code,
            Message = error.Message
        };
    }

    /// <summary>
    /// Asynchronously builds an action content object and retrieves its associated data for a specified workflow run
    /// action.
    /// </summary>
    /// <param name="actionHelper">An implementation of the IActionHelper interface used to obtain workflow run action content and associated data.</param>
    /// <param name="link">The workflow run details action content that identifies the action for which to build content. Can be null.</param>
    /// <returns>A tuple containing the constructed ActionContent object and its associated data as a JToken. Returns (null, null) if link is null.</returns>
    private static async Task<(ActionContent?, JToken?)> BuildContentAsync(IActionHelper actionHelper, WorkflowRunDetailsActionContent? link)
    {
        if (link == null)
            return (null, null);

        var wfActionContent = actionHelper.GetWorkflowRunActionContent(link);

        var content = new ActionContent
        {
            ContentSize = wfActionContent.ContentSize,
            Metadata = new ActionContentMetadata
            {
                ForeachItemsCount = wfActionContent.Metadata?.ForeachItemsCount
            },
            Uri = wfActionContent.Uri
        };

        var data = await actionHelper.GetActionData(wfActionContent).ConfigureAwait(false);

        return (content, data);
    }
}