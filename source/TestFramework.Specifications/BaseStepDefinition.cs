using System.Diagnostics.CodeAnalysis;
using LogicApps.Management.Factory;
using LogicApps.Management.Helper;
using LogicApps.Management.Repository;
using LogicApps.Management.Repository.ServiceBus;
using LogicApps.Management.Repository.StorageAccount;
using LogicApps.TestFramework.Specifications.Helpers;
using LogicApps.TestFramework.Specifications.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Pather.CSharp;
using Reqnroll;
using System.Globalization;
using System.Net.Http.Headers;

namespace LogicApps.TestFramework.Specifications;

[ExcludeFromCodeCoverage(Justification = "This file is tightly coupled with the Azure infrastructure and therefore cannot be tested in unit test.")]
public abstract class BaseStepDefinition : IDisposable
{
    private AzureHttpClient? _logicAppHttpClient;
    private AzureHttpClient? _storageAccountHttpClient;
    private AzureHttpClient? _serviceBusHttpClient;

    private AzureManagementRepository? _logicAppAzureManagementRepository;
    private AzureManagementRepository? _storageAccountAzureManagementRepository;
    private AzureManagementRepository? _serviceBusAzureManagementRepository;

    private IActionHelper? _actionHelper;
    private IActionFactory? _actionFactory;

    private bool _disposedValue;
    private object? _transformedBody;

    private Management.Workflow? _currentWorkflow;
    private List<Management.WorkflowRun> _currentWorkflowRuns = [];

    protected Management.Workflow? CurrentWorkflow => _currentWorkflow;

    protected IList<Management.WorkflowRun> CurrentWorkflowRuns => _currentWorkflowRuns;

    protected IServiceBusMessageSender ServiceBusMessageSender { get; private set; }

    protected IBlobStorageSender BlobStorageSender { get; private set; }

    protected Management.LogicApp? LogicApp { get; set; }

    protected string? CurrentCorrelationId { get; set; }

    protected string? CurrentWorkflowName { get; set; }

    protected BaseStepDefinition()
    {
        var configuration = AppSettings.Configuration;
        var loadRunsSince = DateTime.UtcNow.AddMinutes(-10);

        InitializeLogicAppResources(configuration);
        LogicApp = Management.LogicApp.CreateAsync(configuration, _logicAppAzureManagementRepository!, _actionFactory!, _actionHelper!, loadRunsSince).GetAwaiter().GetResult();

        InitializeStorageAccountResources(configuration);
        BlobStorageSender = new BlobStorageSender(_storageAccountAzureManagementRepository!);

        InitializeServiceBusResources(configuration);
        ServiceBusMessageSender = new ServiceBusMessageSender(_serviceBusAzureManagementRepository!);
    }

    #region Gherkin Steps - When (Triggers)

    [When("Workflow \"(.*)\" is triggered")]
    public async Task WhenWorkFlowIsTriggered(string workflowName)
    {
        CurrentWorkflowName = workflowName;

        var logicAppWorkflows = await LogicApp!.GetWorkflowsAsync().ConfigureAwait(false);
        _currentWorkflow = logicAppWorkflows.FirstOrDefault(workflow => workflow.Name == workflowName);
        var trigger = await _currentWorkflow!.GetTriggerAsync().ConfigureAwait(false);
        await trigger.Run(null).ConfigureAwait(false);

        Management.WorkflowRun? currentRun;
        do
        {
            currentRun = await GetReadyWorkflowRun(_currentWorkflow).ConfigureAwait(false);
        } while (currentRun?.Status == "Running");

        _currentWorkflowRuns = currentRun != null ? [currentRun] : [];
        CurrentCorrelationId = currentRun?.CorrelationId;
    }

    [When("Workflow \"(.*)\" is triggered with file \"(.*)\"")]
    public async Task WhenWorkFlowIsTriggered(string workflowName, string filename)
    {
        CurrentWorkflowName = workflowName;

        var filePath = $"TestData\\{filename}";

        if (File.Exists(filePath))
        {
            var fileContent = File.ReadAllTextAsync(filePath).ConfigureAwait(false).GetAwaiter().GetResult();
            using var content = new StringContent(fileContent);
            _currentWorkflow = (await LogicApp!.GetWorkflowsAsync().ConfigureAwait(false)).FirstOrDefault(workflow => workflow.Name == workflowName);
            var trigger = await _currentWorkflow!.GetTriggerAsync().ConfigureAwait(false);
            await trigger.Run(content).ConfigureAwait(false);

            Management.WorkflowRun? currentRun;
            do
            {
                currentRun = await GetReadyWorkflowRun(_currentWorkflow).ConfigureAwait(false);
            } while (currentRun?.Status == "Running");

            _currentWorkflowRuns = currentRun != null ? [currentRun] : [];
            CurrentCorrelationId = currentRun?.CorrelationId;
        }
    }

    [When("Workflow \"(.*)\" is triggered with json file \"(.*)\"")]
    public async Task WhenWorkFlowIsTriggeredWithJsonContent(string workflowName, string filename)
    {
        ArgumentNullException.ThrowIfNull(filename);

        var filePath = $"TestData\\{filename}";
        var headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" }
        };

        if (File.Exists(filePath))
        {
            var fileContent = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            await WhenWorkFlowIsTriggeredWithContent(workflowName, fileContent, headers).ConfigureAwait(false);
        }
    }

    public async Task WhenWorkFlowIsTriggeredWithContent(string workflowName, string fileContent, Dictionary<string, string>? headers)
    {
        CurrentWorkflowName = workflowName;

        var mediaType = headers?.FirstOrDefault(h => h.Key == "Content-Type").Value ?? "text/plain";
        var headerValue = new MediaTypeHeaderValue(mediaType);

        using var content = new StringContent(fileContent, headerValue);

        if (headers != null)
        {
            foreach (var header in headers.Where(header => !content.Headers.Contains(header.Key)))
            {
                content.Headers.Add(header.Key, header.Value);
            }
        }

        _currentWorkflow = (await LogicApp!.GetWorkflowsAsync().ConfigureAwait(false)).FirstOrDefault(workflow => workflow.Name == workflowName);
        var trigger = await _currentWorkflow!.GetTriggerAsync().ConfigureAwait(false);
        await trigger.Run(content).ConfigureAwait(false);

        Management.WorkflowRun? currentRun;
        do
        {
            currentRun = await GetReadyWorkflowRun(_currentWorkflow).ConfigureAwait(false);
        } while (currentRun?.Status == "Running");

        _currentWorkflowRuns = currentRun != null ? [currentRun] : [];
        CurrentCorrelationId = currentRun?.CorrelationId;
    }

    #endregion

    #region Gherkin Steps - Then (Assertions)

    [Then("The transformed data has \"(.*)\" with value \"(.*)\"")]
    public void ThenTransformedDataHasWithValue(string field, string value)
    {
        var resolver = new Resolver();
        var result = resolver.Resolve(_transformedBody, field);
        Assert.That(Convert.ToString(result, CultureInfo.InvariantCulture), Is.EqualTo(value));
    }

    [Then("The transformed data has \"(.*)\" with values")]
    public void ThenTransformedDataHasWithValue(string field, Table table)
    {
        Assert.That(_transformedBody, Is.Not.Null, "No transformed data is available.");

        var resolver = new Resolver();

        foreach (var row in (table ?? throw new ArgumentNullException(nameof(table))).Rows)
        {
            var expectedField = row["Field"];
            var expectedValue = StringHelper.GetPossibleNullValue(row["Value"]);
            var actualValue = resolver.Resolve(_transformedBody, $"{field}.{expectedField}");

            if (expectedValue != null)
            {
                Assert.That(Convert.ToString(actualValue, CultureInfo.InvariantCulture),
                    Is.EqualTo(Convert.ToString(expectedValue, CultureInfo.InvariantCulture)));
            }
            else
            {
                Assert.That(actualValue, Is.EqualTo(expectedValue));
            }
        }
    }

    [Then("The workflow executed these actions:")]
    public async Task ThenWorkflowExecutedActions(Table table)
    {
        EnsureHasWorkflowRuns();
        var expectedEvents = table.CreateSet<WorkflowEvent>().ToList();

        foreach (var workflowRun in _currentWorkflowRuns)
        {
            var validation = await ValidateRun(workflowRun, expectedEvents).ConfigureAwait(false);
            Assert.That(validation.Item1, Is.True, validation.Item2);
        }
    }

    [Then("The workflow \"(.*)\" executed these actions:")]
    public async Task ThenWorkflowExecutedActionsWithName(string workflowName, Table table)
    {
        // This step specifies a different workflow, so we need to fetch it
        CurrentWorkflowName = workflowName;
        _currentWorkflow = (await LogicApp!.GetWorkflowsAsync().ConfigureAwait(false)).FirstOrDefault(w => w.Name == workflowName);

        var workflowRun = await GetReadyWorkflowRun(_currentWorkflow!).ConfigureAwait(false);
        _currentWorkflowRuns = [workflowRun!];
        
        var expectedEvents = table.CreateSet<WorkflowEvent>().ToList();

        foreach (var currentWorkflowRun in _currentWorkflowRuns)
        {
            var validation = await ValidateRun(currentWorkflowRun, expectedEvents).ConfigureAwait(false);
            Assert.That(validation.Item1, Is.True, validation.Item2);
        }
    }

    [Then("The \"(.*)\" action has status \"(.*)\"")]
    public async Task ThenActionHasStatus(string actionName, string expectedStatus)
    {
        EnsureHasWorkflowRuns();

        foreach (var workflowRun in _currentWorkflowRuns)
        {
            var validation = new WorkflowRunValidation(workflowRun);
            var result = await validation.ValidateSingleActionAsync(actionName, expectedStatus).ConfigureAwait(false);
            Assert.That(result.Item1, Is.True, result.Item2);
        }
    }

    [Then("The \"(.*)\" loop ran (\\d+) times with status \"(.*)\"")]
    public async Task ThenLoopRan(string loopName, int iterations, string status)
    {
        EnsureHasWorkflowRuns();

        foreach (var workflowRun in _currentWorkflowRuns)
        {
            var validation = new WorkflowRunValidation(workflowRun);
            var result = await validation.ValidateLoopIterationCountAsync(loopName, iterations, status).ConfigureAwait(false);
            Assert.That(result.Item1, Is.True, result.Item2);
        }
    }

    [Then("Each iteration of \"(.*)\" executed:")]
    [Then("All iterations of \"(.*)\" executed:")]
    public async Task ThenEachIterationExecuted(string loopName, Table table)
    {
        EnsureHasWorkflowRuns();
        var expectedEvents = table.CreateSet<WorkflowEvent>().ToList();

        foreach (var workflowRun in _currentWorkflowRuns)
        {
            var validation = new WorkflowRunValidation(workflowRun);
            var result = await validation.ValidateActionsInAllIterationsAsync(loopName, expectedEvents).ConfigureAwait(false);
            Assert.That(result.Item1, Is.True, result.Item2);
        }
    }

    [Given("In \"(.*)\":")]
    [When("In \"(.*)\":")]
    [Then("In \"(.*)\":")]
    public async Task ThenInPath(string path, Table table)
    {
        EnsureHasWorkflowRuns();
        var expectedEvents = table.CreateSet<WorkflowEvent>().ToList();

        foreach (var workflowRun in _currentWorkflowRuns)
        {
            var allActions = await workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);

            Console.WriteLine($"Navigating to path: {path} and looking for these actions:");
            foreach (var action in allActions)
            {
                Console.WriteLine($"  - Name: '{action.Name}', DesignerName: '{action.DesignerName}', Type: {action.GetType().Name}");
            }

            var actionsAtPath = ActionPathNavigator.NavigateToPath(allActions, path);

            Console.WriteLine($"The actual actions found at path '{path}': {actionsAtPath.Count}");
            foreach (var action in actionsAtPath)
            {
                Console.WriteLine($"  - Found: '{action.DesignerName}' ({action.Name}) - Status: {action.Status}");
            }

            if (actionsAtPath.Count == 0)
            {
                Assert.Fail($"No actions found at path: {path}\nAvailable top-level actions: {string.Join(", ", allActions.Where(a => !string.IsNullOrEmpty(a.DesignerName)).Select(a => a.DesignerName))}");
                return;
            }

            foreach (var expectedEvent in expectedEvents)
            {
                Console.WriteLine($"Looking for action: '{expectedEvent.StepName}' with status '{expectedEvent.Status}'");

                var action = actionsAtPath.FirstOrDefault(a =>
                    a.DesignerName == expectedEvent.StepName ||
                    a.Name == expectedEvent.StepName);

                if (action == null)
                {
                    var availableNames = string.Join(", ", actionsAtPath.Select(a => $"'{a.DesignerName ?? a.Name}'"));
                    Assert.Fail($"Action '{expectedEvent.StepName}' not found at path '{path}'.\nAvailable actions at this path: {availableNames}");
                    return;
                }

                if (!action.Status!.Equals(expectedEvent.Status, StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Fail($"Action '{expectedEvent.StepName}' has status '{action.Status}', but expected '{expectedEvent.Status}'");
                    return;
                }

                Console.WriteLine($"Found '{expectedEvent.StepName}' with correct status '{expectedEvent.Status}'");
            }
        }
    }

    [Then("In iteration (\\d+) of \"(.*)\":")]
    [Then("Iteration (\\d+) of \"(.*)\" executed:")]
    public async Task ThenInIterationOf(int iteration, string loopName, Table table)
    {
        EnsureHasWorkflowRuns();
        var expectedEvents = table.CreateSet<WorkflowEvent>().ToList();

        foreach (var workflowRun in _currentWorkflowRuns)
        {
            var validation = new WorkflowRunValidation(workflowRun);
            var result = await validation.ValidateActionsInIterationAsync(loopName, iteration, expectedEvents).ConfigureAwait(false);
            Assert.That(result.Item1, Is.True, result.Item2);
        }
    }

    [Then("Within \"(.*)\":")]
    [Then("In scope \"(.*)\":")]
    public async Task ThenWithinScope(string scopeName, Table table)
    {
        EnsureHasWorkflowRuns();
        var expectedEvents = table.CreateSet<WorkflowEvent>().ToList();

        foreach (var workflowRun in _currentWorkflowRuns)
        {
            var validation = new WorkflowRunValidation(workflowRun);
            var result = await validation.ValidateChildActionsAsync(scopeName, expectedEvents).ConfigureAwait(false);
            Assert.That(result.Item1, Is.True, result.Item2);
        }
    }

    [Then("In the \"(.*)\" branch of \"(.*)\":")]
    [Then("In condition \"(.*)\" branch \"(.*)\":")]
    public async Task ThenInBranchOf(string branch, string conditionName, Table table)
    {
        EnsureHasWorkflowRuns();
        var expectedEvents = table.CreateSet<WorkflowEvent>().ToList();

        foreach (var workflowRun in _currentWorkflowRuns)
        {
            var validation = new WorkflowRunValidation(workflowRun);
            var result = await validation.ValidateChildActionsAsync(conditionName, expectedEvents, branch).ConfigureAwait(false);
            Assert.That(result.Item1, Is.True, result.Item2);
        }
    }

    [Then("The nested \"(.*)\" loop in iteration (\\d+) of \"(.*)\" ran (\\d+) times with status \"(.*)\"")]
    public async Task ThenNestedLoopRan(string nestedLoopName, int parentIteration, string parentLoopName, int iterations, string status)
    {
        EnsureHasWorkflowRuns();

        foreach (var workflowRun in _currentWorkflowRuns)
        {
            var validation = new WorkflowRunValidation(workflowRun);
            var result = await validation.ValidateNestedLoopAsync(parentLoopName, parentIteration, nestedLoopName, iterations, status).ConfigureAwait(false);
            Assert.That(result.Item1, Is.True, result.Item2);
        }
    }

    [Then("For all instances of \"(.*)\" with the same correlation:")]
    public async Task ThenForAllCorrelatedInstances(string workflowName, Table table)
    {
        CurrentWorkflowName = workflowName;
        _currentWorkflow = (await LogicApp!.GetWorkflowsAsync().ConfigureAwait(false)).FirstOrDefault(w => w.Name == workflowName);

        do
        {
            _currentWorkflowRuns = [.. (await GetCorrelatedWorkflowRuns(_currentWorkflow!).ConfigureAwait(false))];
        } while (_currentWorkflowRuns.Count == 0 || _currentWorkflowRuns.Any(run => run.Status == "Running"));

        var expectedEvents = table.CreateSet<WorkflowEvent>().ToList();

        foreach (var workflowRun in _currentWorkflowRuns)
        {
            var validation = await ValidateRun(workflowRun, expectedEvents).ConfigureAwait(false);
            Assert.That(validation.Item1, Is.True, validation.Item2);
        }
    }

    #endregion

    #region Protected Helpers

    /// <summary>
    /// Sets the transformed body to the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the transformed body to set.</typeparam>
    /// <param name="transformedBody">The value to assign as the transformed body.</param>
    protected void SetTransformedBody<T>(T transformedBody)
    {
        _transformedBody = transformedBody;
    }

    /// <summary>
    /// Asynchronously retrieves the workflow run that is ready for processing, based on the current correlation identifier if available.
    /// </summary>
    /// <remarks>If the current correlation identifier is set, the method returns the workflow run with a matching correlation identifier.
    /// If not set, it returns the workflow run with the latest start time. The method waits for a short period and reloads the workflow before retrieving the runs.</remarks>
    /// <param name="workflow">The workflow instance from which to retrieve the workflow run. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the workflow run that matches the
    /// current correlation identifier if set; otherwise, the most recent workflow run, or null if no runs are available.</returns>
    protected async Task<Management.WorkflowRun?> GetReadyWorkflowRun(Management.Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        await Task.Delay(3000).ConfigureAwait(false);
        await workflow.ReloadAsync().ConfigureAwait(false);

        var workflowRun = CurrentCorrelationId != null ?
            (await workflow.GetWorkflowRunsAsync().ConfigureAwait(false)).FirstOrDefault(run => run.CorrelationId == CurrentCorrelationId) :
            (await workflow.GetWorkflowRunsAsync().ConfigureAwait(false)).MaxBy(run => run.StartTime);

        return workflowRun;
    }

    /// <summary>
    /// Asynchronously retrieves all workflow runs for the specified workflow that share the current correlation identifier.
    /// </summary>
    /// <remarks>This method reloads the workflow before retrieving its runs to ensure the latest state is used.
    /// Only runs with a correlation identifier matching the current context are returned.</remarks>
    /// <param name="workflow">The workflow instance from which to retrieve correlated workflow runs. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of workflow runs that
    /// have the same correlation identifier as the current context.</returns>
    protected async Task<IEnumerable<Management.WorkflowRun>> GetCorrelatedWorkflowRuns(Management.Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        await Task.Delay(3000).ConfigureAwait(false);
        await workflow.ReloadAsync().ConfigureAwait(false);
        var workflowRuns = (await workflow.GetWorkflowRunsAsync().ConfigureAwait(false)).Where(run => run.CorrelationId == CurrentCorrelationId);

        return workflowRuns;
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Ensures that at least one workflow run is available before proceeding.
    /// </summary>
    /// <remarks>Call this method before performing operations that require existing workflow runs. This helps
    /// prevent invalid operations when no workflows have been initiated.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if no workflow runs are available. This typically indicates that a workflow has not been triggered yet.</exception>
    protected void EnsureHasWorkflowRuns()
    {
        if (_currentWorkflowRuns.Count == 0)
        {
            throw new InvalidOperationException("No workflow runs are available. Make sure to trigger a workflow first using a 'When Workflow is triggered' step.");
        }
    }

    /// <summary>
    /// Validates the specified workflow run and its actions, optionally retrying the validation if an error occurs.
    /// </summary>
    /// <remarks>If validation fails, the method retries up to three times, reloading the workflow run between attempts.
    /// Throws an exception if validation fails after the maximum number of retries.</remarks>
    /// <param name="workflowRun">The workflow run to validate. Must not be null.</param>
    /// <param name="expectedEvents">An optional list of expected workflow events to validate against. If null, all events are considered valid.</param>
    /// <param name="retry">The current retry attempt count. Must be zero or greater. Used internally to limit the number of retries.</param>
    /// <returns>A tuple containing a Boolean value that is <see langword="true"/> if the workflow run is valid; otherwise, <see
    /// langword="false"/>. The second item is an optional error message if validation fails.</returns>
    private static async Task<(bool, string?)> ValidateRun(Management.WorkflowRun workflowRun, IList<WorkflowEvent>? expectedEvents = null, int retry = 0)
    {
        try
        {
            var workflowRunValidation = new WorkflowRunValidation(workflowRun);
            var result = await workflowRunValidation.ValidateRunActionsAsync(expectedEvents).ConfigureAwait(false);

            return !result.Item1 ? throw new ArgumentException(result.Item2) : result;
        }
        catch
        {
            if (retry >= 3)
            {
                throw;
            }

            await Task.Delay(retry * 3000).ConfigureAwait(false);
            await workflowRun.Reload().ConfigureAwait(false);
            return await ValidateRun(workflowRun, expectedEvents, retry + 1).ConfigureAwait(false);
        }
    }

    #endregion

    #region Infrastructure

    /// <summary>
    /// Initializes resources required for interacting with Azure Logic Apps using the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration settings used to initialize Logic App resources. Cannot be null.</param>
    private void InitializeLogicAppResources(IConfiguration configuration)
    {
        var baseAddress = new Uri("https://management.azure.com");
        var (azureHttpClient, repository) = CreateAzureRepository(configuration, baseAddress);

        _logicAppHttpClient = azureHttpClient;
        _logicAppAzureManagementRepository = repository;

        _actionHelper = new ActionHelper(_logicAppAzureManagementRepository);
        _actionFactory = new ActionFactory(configuration, _logicAppAzureManagementRepository, _actionHelper);
    }

    /// <summary>
    /// Initializes resources required for accessing the Azure Storage account using the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration settings used to determine the storage account and initialize related resources. Must contain
    /// a valid 'StorageAccount' entry.</param>
    private void InitializeStorageAccountResources(IConfiguration configuration)
    {
        var baseAddress = new Uri($"https://{configuration["StorageAccount"]!}.blob.core.windows.net");
        var (azureHttpClient, repository) = CreateAzureRepository(configuration, baseAddress);

        _storageAccountHttpClient = azureHttpClient;
        _storageAccountAzureManagementRepository = repository;
    }

    /// <summary>
    /// Initializes the Service Bus HTTP client and Azure management repository using the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration settings used to determine the Service Bus namespace and other required parameters.
    /// Cannot be null.</param>
    private void InitializeServiceBusResources(IConfiguration configuration)
    {
        var baseAddress = new Uri($"https://{configuration["ServiceBusNamespace"]!}.servicebus.windows.net");
        var (azureHttpClient, repository) = CreateAzureRepository(configuration, baseAddress);

        _serviceBusHttpClient = azureHttpClient;
        _serviceBusAzureManagementRepository = repository;
    }

    /// <summary>
    /// Configures the specified HTTP client with a base address, default headers, version policy, and timeout suitable for JSON-based APIs.
    /// </summary>
    /// <remarks>Sets the client's base address, configures the default request version policy to use the requested version or lower,
    /// adds an 'Accept: application/json' header, and sets the timeout to 30 minutes.</remarks>
    /// <param name="client">The HTTP client instance to configure. Must not be null.</param>
    /// <param name="baseUri">The base URI to assign to the HTTP client. Must be an absolute URI.</param>
    private static void ConfigureHttpClient(HttpClient client, Uri baseUri)
    {
        client.BaseAddress = baseUri;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        client.DefaultRequestHeaders.Add("accept", "application/json");
        client.Timeout = TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Creates and configures the Azure management repository and its supporting HTTP clients and token client using the specified configuration and base address.
    /// </summary>
    /// <remarks>The returned services are fully configured and ready for use with Azure management APIs.
    /// The configuration must include valid Azure credentials. The caller is responsible for managing the lifetime of the returned objects as needed.</remarks>
    /// <param name="configuration">The application configuration containing required Azure credentials and settings.
    /// Must provide values for 'TenantId', 'ClientId', and 'ClientSecret'.</param>
    /// <param name="baseAddress">The base URI for Azure management API requests.</param>
    /// <returns>A tuple containing the HTTP client factory, token client, Azure HTTP client, and Azure management repository,
    /// all configured for Azure management operations.</returns>
    private static (AzureHttpClient azureHttpClient, AzureManagementRepository repository) CreateAzureRepository(IConfiguration configuration, Uri baseAddress)
    {
        var services = new ServiceCollection();

        services.AddHttpClient("AzureManagementClient", client =>
        {
            ConfigureHttpClient(client, baseAddress);
        });

        services.AddHttpClient("AzurePublicHttpClient", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        services.AddHttpClient("EntraTokenClient");

        var serviceProvider = services.BuildServiceProvider();

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var tokenClient = new EntraTokenClient(httpClientFactory);

        var azureHttpClient = new AzureHttpClient(httpClientFactory, tokenClient, baseAddress, configuration["TenantId"]!, configuration["ClientId"]!, configuration["ClientSecret"]!);
        var repository = new AzureManagementRepository(azureHttpClient, baseAddress);

        return (azureHttpClient, repository);
    }

    #endregion

    /// <summary>
    /// Releases all resources used by the current instance of the class.
    /// </summary>
    /// <remarks>Call this method when you are finished using the object to free unmanaged resources and
    /// perform other cleanup operations. After calling this method, the object should not be used.</remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the object and, optionally, releases the managed resources.
    /// </summary>
    /// <remarks>This method is called by public Dispose methods and the finalizer. When disposing is true,
    /// this method releases all resources held by managed objects. Override this method to release additional resources in a derived class.</remarks>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue) return;

        if (disposing)
        {
            _actionHelper = null;
            _actionFactory = null;

            _logicAppHttpClient?.Dispose();
            _logicAppAzureManagementRepository?.Dispose();

            _storageAccountHttpClient?.Dispose();
            _storageAccountAzureManagementRepository?.Dispose();

            _serviceBusHttpClient?.Dispose();
            _serviceBusAzureManagementRepository?.Dispose();
        }

        _disposedValue = true;
    }
}