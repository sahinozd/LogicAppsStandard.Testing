using LogicApps.Management.Factory;
using LogicApps.Management.Helper;
using LogicApps.Management.Models.Constants;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace LogicApps.Management;

/// <summary>
/// Represents a Logic App workflow and provides methods to load its definition and associated runs.
/// </summary>
public sealed class Workflow : IWorkflow
{
    private readonly IConfiguration _configuration;
    private readonly IAzureManagementRepository _azureManagementRepository;
    private readonly IActionHelper _actionHelper;
    private readonly IActionFactory _actionFactory;

    private readonly Models.RestApi.Workflow _workflowProperties;
    private readonly DateTime? _loadRunsSince;
    private List<IWorkflowRun>? _workflowRuns;
    private IWorkflowTrigger? _trigger;

    public string? FullName { get; private set; }

    public string? Id { get; private set; }

    public string? Kind { get; private set; }

    public string? Location { get; private set; }

    public string? Name { get; private set; }

    public string? Type { get; private set; }

    public JObject? Definition { get; private set; }

    private Workflow(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionFactory actionFactory, IActionHelper actionHelper, Models.RestApi.Workflow workflowProperties, DateTime? loadRunsSince)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _azureManagementRepository = azureManagementRepository ?? throw new ArgumentNullException(nameof(azureManagementRepository));
        _actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
        _actionHelper = actionHelper ?? throw new ArgumentNullException(nameof(actionHelper));
        _workflowProperties = workflowProperties ?? throw new ArgumentNullException(nameof(workflowProperties));
        _loadRunsSince = loadRunsSince;
    }

    /// <summary>
    /// Factory method that creates and initializes a <see cref="Workflow"/> instance from API-provided workflow properties.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="azureManagementRepository">Repository used to access management API.</param>
    /// <param name="actionFactory">Factory used to create action instances from workflow definitions.</param>
    /// <param name="actionHelper">Helper to fetch linked action content.</param>
    /// <param name="workflowProperties">API-provided workflow metadata.</param>
    /// <param name="loadRunsSince">Optional date to restrict run loading.</param>
    /// <returns>An initialized <see cref="IWorkflow"/> instance.</returns>
    public static async Task<IWorkflow> CreateAsync(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionFactory actionFactory, IActionHelper actionHelper, Models.RestApi.Workflow workflowProperties, DateTime? loadRunsSince)
    {
        var workflow = new Workflow(configuration, azureManagementRepository, actionFactory, actionHelper, workflowProperties, loadRunsSince);
        return await workflow.InitializeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Get the workflow's trigger instance, loading it from the management API on first access.
    /// </summary>
    /// <returns>The workflow's <see cref="WorkflowTrigger"/> instance.</returns>
    public async Task<IWorkflowTrigger> GetTriggerAsync()
    {
        return _trigger ??= await WorkflowTrigger.CreateAsync(_configuration, _azureManagementRepository, Name!).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves workflow runs from the management API. Results are cached in the instance until <see cref="ReloadAsync"/> is called.
    /// </summary>
    /// <returns>List of <see cref="WorkflowRun"/> instances for this workflow.</returns>
    public async Task<List<IWorkflowRun>> GetWorkflowRunsAsync(CancellationToken cancellationToken = default)
    {
        if (_workflowRuns is { Count: > 0 })
        {
            return _workflowRuns;
        }

        var dateFilter = string.Empty;

        if (_loadRunsSince != null)
        {
            var date = _loadRunsSince.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
            dateFilter = $"&$filter=startTime ge {date}";
        }

        var uriString = $"/subscriptions/{_configuration[StringConstants.SubscriptionId]!}/resourceGroups/{_configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{_configuration[StringConstants.LogicAppName]!}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{Name!}/runs?api-version={_configuration[StringConstants.LogicAppApiVersion]!}{dateFilter}";
        var runs = new List<Models.RestApi.WorkflowRun>();

        while (!string.IsNullOrEmpty(uriString))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _azureManagementRepository.GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(new Uri(uriString, UriKind.Relative)).ConfigureAwait(false);
            if (result?.Value != null)
            {
                runs.AddRange(result.Value);
            }

            uriString = !string.IsNullOrEmpty(result?.NextLink) ? new Uri(result.NextLink).PathAndQuery : string.Empty;
        }

        var workflowRuns = new List<IWorkflowRun>();
        foreach (var run in runs)
        {
            var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, Name!, run, Definition!).ConfigureAwait(false);
            workflowRuns.Add(workflowRun);
        }

        _workflowRuns = workflowRuns;
        return _workflowRuns;
    }

    /// <summary>
    /// Returns the first workflow run matching the specified correlation ID, or null if none is found.
    /// </summary>
    /// <param name="correlationId">The correlation ID to match against workflow runs.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The matching <see cref="IWorkflowRun"/>, or null.</returns>
    public async Task<IWorkflowRun?> GetWorkflowRunByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(correlationId);

        var runs = await GetWorkflowRunsAsync(cancellationToken).ConfigureAwait(false);
        return runs.FirstOrDefault(r => r.CorrelationId == correlationId);
    }

    /// <summary>
    /// Returns the workflow run with the specified Azure-assigned run ID, or null if none is found.
    /// </summary>
    /// <param name="runId">The Azure run ID (the <c>Name</c> field on the run resource).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching <see cref="IWorkflowRun"/>, or null.</returns>
    public async Task<IWorkflowRun?> GetWorkflowRunByIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(runId);

        var runs = await GetWorkflowRunsAsync(cancellationToken).ConfigureAwait(false);
        return runs.FirstOrDefault(r => r.Name == runId);
    }

    /// <summary>
    /// Clears cached run data and reloads runs from the management API.
    /// </summary>
    /// <returns>A task representing the reload operation.</returns>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        _workflowRuns = null;
        await GetWorkflowRunsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Load workflow metadata and definition from the API and populate instance properties.
    /// </summary>
    /// <returns>The initialized <see cref="Workflow"/> instance.</returns>
    private async Task<Workflow> InitializeAsync()
    {
        FullName = _workflowProperties.Name;
        Id = _workflowProperties.Id;
        Kind = _workflowProperties.Kind;
        Location = _workflowProperties.Location;
        Name = FullName?[(FullName.LastIndexOf('/') + 1)..];
        Type = _workflowProperties.Type;
        Definition = await LoadWorkflowDefinitionAsync().ConfigureAwait(false);
        
        return this;
    }

    /// <summary>
    /// Load the workflow definition (workflow.json) for the workflow and return it as a <see cref="JObject"/>.
    /// </summary>
    /// <returns>The workflow definition or null if not present.</returns>
    private async Task<JObject?> LoadWorkflowDefinitionAsync()
    {
        var relativeUri = new Uri($"/subscriptions/{_configuration[StringConstants.SubscriptionId]!}/resourceGroups/{_configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{_configuration[StringConstants.LogicAppName]!}/workflows/{Name}?api-version={_configuration[StringConstants.LogicAppApiVersion]!}", UriKind.Relative);
        var result = await _azureManagementRepository.GetObjectAsync<JObject>(relativeUri).ConfigureAwait(false);

        var definition = result?.SelectToken("properties.files['workflow.json'].definition");
        var definitionResult = definition as JObject;
        return definitionResult;
    }
}