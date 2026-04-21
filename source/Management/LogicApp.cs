using LogicApps.Management.Factory;
using LogicApps.Management.Helper;
using LogicApps.Management.Models.Constants;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;

namespace LogicApps.Management;

/// <summary>
/// Represents the top-level Logic App instance and provides methods to enumerate workflows and initialize runtime state.
/// The class wraps access to the Azure Management API and coordinates creation of workflow and action models.
/// </summary>
public sealed class LogicApp
{
    private readonly IConfiguration _configuration;
    private readonly IAzureManagementRepository _azureManagementRepository;
    private readonly IActionHelper _actionHelper;
    private readonly IActionFactory _actionFactory;

    private readonly DateTime? _loadRunsSince;
    private List<Workflow>? _workflows;

    public string? Id { get; private set; }

    public string? Kind { get; private set; }

    public string? Location { get; private set; }

    public string? Name { get; private set; }

    public string? Type { get; private set; }

    private LogicApp(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionFactory actionFactory, IActionHelper actionHelper, DateTime? loadRunsSince)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _azureManagementRepository = azureManagementRepository ?? throw new ArgumentNullException(nameof(azureManagementRepository));
        _actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
        _actionHelper = actionHelper ?? throw new ArgumentNullException(nameof(actionHelper));
        _loadRunsSince = loadRunsSince;
    }

    /// <summary>
    /// Creates an instance of a Logic App Standard.
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="azureManagementRepository">Repository object</param>
    /// <param name="actionFactory">ActionFactory object</param>
    /// <param name="actionHelper">ActionHelper object</param>
    /// <param name="loadRunsSince">Start date to retrieve runs from</param>
    /// <returns>A logic app standard object</returns>
    public static Task<LogicApp> CreateAsync(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionFactory actionFactory, IActionHelper actionHelper, DateTime? loadRunsSince)
    {
        var logicApp = new LogicApp(configuration, azureManagementRepository, actionFactory, actionHelper, loadRunsSince);
        return logicApp.InitializeAsync();
    }

    /// <summary>
    /// Asynchronously retrieves the list of workflows associated with the configured Logic App.
    /// </summary>
    /// <remarks>Subsequent calls may return cached results to improve performance. The returned workflows
    /// reflect the state at the time of retrieval and may not include changes made after the method completes.</remarks>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of workflows for the current
    /// Logic App. The list is empty if no workflows are found.</returns>
    public async Task<List<Workflow>> GetWorkflowsAsync()
    {
        if (_workflows is { Count: > 0 })
        {
            return _workflows;
        }

        var relativeUri = new Uri($"/subscriptions/{_configuration[StringConstants.SubscriptionId]!}/resourceGroups/{_configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{_configuration[StringConstants.LogicAppName]!}/workflows?api-version={_configuration[StringConstants.LogicAppApiVersion]!}", UriKind.Relative);
        var result = await _azureManagementRepository.GetObjectAsync<Response<Models.RestApi.Workflow>>(relativeUri).ConfigureAwait(false);

        var workflowTasks = result!.Value?.Select(workflowProperties => Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, workflowProperties, _loadRunsSince)) ?? [];
        _workflows = [.. await Task.WhenAll(workflowTasks).ConfigureAwait(false)];

        return _workflows;
    }

    /// <summary>
    /// Retrieves information about the specified Logic App and updates the corresponding properties with the retrieved
    /// values.
    /// </summary>
    /// <remarks>This method updates the Id, Kind, Location, Name, and Type properties based on the Logic App
    /// information retrieved from Azure. The method must be awaited to ensure that the properties are updated before
    /// they are accessed.</remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task GetLogicAppInformationAsync()
    {
        var relativeUri = new Uri($"/subscriptions/{_configuration[StringConstants.SubscriptionId]!}/resourceGroups/{_configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{_configuration[StringConstants.LogicAppName]!}?api-version={_configuration[StringConstants.LogicAppApiVersion]!}", UriKind.Relative);
        var result = await _azureManagementRepository.GetObjectAsync<Models.RestApi.LogicApp>(relativeUri).ConfigureAwait(false);

        Id = result?.Id;
        Kind = result?.Kind;
        Location = result?.Location;
        Name = result?.Name;
        Type = result?.Type;
    }

    /// <summary>
    /// Asynchronously initializes the current instance and retrieves logic app information.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the initialized instance of the
    /// logic app.</returns>
    private async Task<LogicApp> InitializeAsync()
    {
        await GetLogicAppInformationAsync().ConfigureAwait(false);
        return this;
    }
}