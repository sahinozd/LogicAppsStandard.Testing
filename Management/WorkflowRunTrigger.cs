using LogicApps.Management.Helper;
using LogicApps.Management.Models.Constants;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace LogicApps.Management;

/// <summary>
/// Represents the trigger information for a single workflow run. This type loads trigger metadata and any
/// linked inputs/outputs from the management API and exposes them as strong-typed properties.
/// </summary>
public sealed class WorkflowRunTrigger
{
    private readonly IConfiguration _configuration;
    private readonly IAzureManagementRepository _azureManagementRepository;
    private readonly IActionHelper _actionHelper;
    private readonly string _workflowName;
    private readonly string _runId;

    public Correlation? Correlation { get; set; }

    public string? DesignerName { get; private set; }

    public DateTime? EndTime { get; set; }

    public JToken? Input { get; private set; }

    public WorkflowRunDetailsActionContent? InputsLink { get; private set; }

    public string? Name { get; set; }

    public string? OriginHistoryName { get; set; }

    public JToken? Output { get; private set; }

    public WorkflowRunDetailsActionContent? OutputsLink { get; private set; }

    public DateTime? StartTime { get; set; }

    public string? Status { get; set; }

    /// <summary>
    /// Create a new instance of <see cref="WorkflowRunTrigger"/>. The instance is initialized by calling <see cref="InitializeAsync"/> via <see cref="CreateAsync"/>.
    /// </summary>
    private WorkflowRunTrigger(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionHelper actionHelper, string workflowName, string runId)// : base(azureManagementRepository)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _azureManagementRepository = azureManagementRepository ?? throw new ArgumentNullException(nameof(azureManagementRepository));
        _actionHelper = actionHelper ?? throw new ArgumentNullException(nameof(actionHelper));
        _workflowName = workflowName ?? throw new ArgumentNullException(nameof(workflowName));
        _runId = runId ?? throw new ArgumentNullException(nameof(runId));
    }

    /// <summary>
    /// Create and asynchronously initialize a <see cref="WorkflowRunTrigger"/> for the specified workflow run.
    /// </summary>
    /// <param name="configuration">Configuration instance.</param>
    /// <param name="azureManagementRepository">Repository used to call the management API.</param>
    /// <param name="actionHelper">Helper used to fetch linked action content.</param>
    /// <param name="workflowName">Name of the workflow.</param>
    /// <param name="runId">Identifier of the workflow run.</param>
    /// <returns>An initialized <see cref="WorkflowRunTrigger"/> instance.</returns>
    public static Task<WorkflowRunTrigger> CreateAsync(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionHelper actionHelper, string workflowName, string runId)
    {
        var ret = new WorkflowRunTrigger(configuration, azureManagementRepository, actionHelper, workflowName, runId);
        return ret.InitializeAsync();
    }

    /// <summary>
    /// Internal asynchronous initializer. Loads trigger properties and linked content.
    /// </summary>
    /// <returns>The initialized <see cref="WorkflowRunTrigger"/> instance.</returns>
    private async Task<WorkflowRunTrigger> InitializeAsync()
    {
        await SetPropertiesAsync().ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Load trigger metadata from the management API for this workflow run and populate instance properties including
    /// correlation, inputs/outputs and timing information.
    /// </summary>
    private async Task SetPropertiesAsync()
    {
        // This is the only request that uses the old 2018 api version.
        var relativeUri = new Uri($"/subscriptions/{_configuration[StringConstants.SubscriptionId]!}/resourceGroups/{_configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{_configuration[StringConstants.LogicAppName]!}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{_workflowName}/runs/{_runId}?api-version=2025-05-01", UriKind.Relative);
        var result = await _azureManagementRepository.GetObjectAsync<WorkflowRunDetails>(relativeUri).ConfigureAwait(false);
        var trigger = result?.Properties?.Trigger;

        DesignerName = trigger?.Name?.Replace("_", " ", StringComparison.OrdinalIgnoreCase);
        EndTime = trigger?.EndTime;
        Name = trigger?.Name;
        OriginHistoryName = trigger?.OriginHistoryName;
        StartTime = trigger?.StartTime;
        Status = trigger?.Status;

        if (trigger?.Correlation != null)
        {
            Correlation = new Correlation
            {
                ClientTrackingId = trigger.Correlation.ClientTrackingId,
                ActionTrackingId = trigger.Correlation.ActionTrackingId,
            };
        }
        
        if (trigger?.InputsLink != null)
        {
            InputsLink = _actionHelper.GetWorkflowRunActionContent(trigger.InputsLink);
            Input = await _actionHelper.GetActionData(InputsLink).ConfigureAwait(false);
        }

        if (trigger?.OutputsLink != null)
        {
            OutputsLink = _actionHelper.GetWorkflowRunActionContent(trigger.OutputsLink);
            Output = await _actionHelper.GetActionData(OutputsLink).ConfigureAwait(false);
        }
    }
}