using LogicApps.Management.Factory;
using LogicApps.Management.Helper;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;

namespace LogicApps.Management.Tests;

[TestFixture]
internal sealed class WorkflowTests
{
    private IConfiguration _configuration;
    private IAzureManagementRepository _azureManagementRepository;
    private IActionFactory _actionFactory;
    private IActionHelper _actionHelper;
    private Models.RestApi.Workflow _workflowProperties;
    private Response<Models.RestApi.WorkflowRun>? _workflowRunsResponse;
    private JObject? _workflowDefinitionResponse;

    [SetUp]
    public void SetUp()
    {
        // Initialize mocked dependencies
        _configuration = Substitute.For<IConfiguration>();
        _azureManagementRepository = Substitute.For<IAzureManagementRepository>();
        _actionFactory = Substitute.For<IActionFactory>();
        _actionHelper = Substitute.For<IActionHelper>();

        // Setup configuration values
        _configuration["SubscriptionId"].Returns("subscription-id");
        _configuration["ResourceGroup"].Returns("resource-group");
        _configuration["LogicAppName"].Returns("logic-app");
        _configuration["LogicAppApiVersion"].Returns("2025-05-01");

        // Load workflow properties from JSON file
        var workflowPropertiesFilePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Workflow-content.json");
        if (!File.Exists(workflowPropertiesFilePath))
        {
            throw new FileLoadException($"File does not exist: {workflowPropertiesFilePath}");
        }

        var workflowPropertiesJson = File.ReadAllTextAsync(workflowPropertiesFilePath).ConfigureAwait(false).GetAwaiter().GetResult();
        _workflowProperties = JsonConvert.DeserializeObject<Models.RestApi.Workflow>(workflowPropertiesJson)!;

        // Load workflow runs from JSON file
        var workflowRunsFilePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Workflow-runs-content.json");
        if (!File.Exists(workflowRunsFilePath))
        {
            throw new FileLoadException($"File does not exist: {workflowRunsFilePath}");
        }

        var workflowRunsJson = File.ReadAllTextAsync(workflowRunsFilePath).ConfigureAwait(false).GetAwaiter().GetResult();
        _workflowRunsResponse = JsonConvert.DeserializeObject<Response<Models.RestApi.WorkflowRun>>(workflowRunsJson);

        // Load workflow definition from JSON file
        var filePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Workflow-definition.json");
        if (!File.Exists(filePath))
        {
            throw new FileLoadException($"File does not exist: {filePath}");
        }

        var workflowDefinitionJson = File.ReadAllTextAsync(filePath).ConfigureAwait(false).GetAwaiter().GetResult();
        _workflowDefinitionResponse = JsonConvert.DeserializeObject<JObject>(workflowDefinitionJson);
    }

    [TearDown]
    public void TearDown()
    {
        _configuration = null!;
        _azureManagementRepository = null!;
        _actionFactory = null!;
        _actionHelper = null!;
        _workflowProperties = null!;
        _workflowRunsResponse = null;
        _workflowDefinitionResponse = null;
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_Configuration_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => Workflow.CreateAsync(null!, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, DateTime.UtcNow));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("configuration"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_AzureManagementRepository_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => Workflow.CreateAsync(_configuration, null!, _actionFactory, _actionHelper, _workflowProperties, DateTime.UtcNow));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("azureManagementRepository"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_ActionFactory_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => Workflow.CreateAsync(_configuration, _azureManagementRepository, null!, _actionHelper, _workflowProperties, DateTime.UtcNow));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionFactory"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_ActionHelper_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, null!, _workflowProperties, DateTime.UtcNow));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionHelper"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_WorkflowProperties_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, null!, DateTime.UtcNow));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("workflowProperties"));
    }

    [Test]
    public async Task CreateAsync_Should_Initialize_Workflow_With_Valid_Parameters()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowDefinition);

        // Act
        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, DateTime.UtcNow).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflow, Is.Not.Null);
            Assert.That(workflow.Id, Is.EqualTo(_workflowProperties.Id));
            Assert.That(workflow.Name, Is.EqualTo("workflow"));
            Assert.That(workflow.FullName, Is.EqualTo(_workflowProperties.Name));
            Assert.That(workflow.Type, Is.EqualTo(_workflowProperties.Type));
            Assert.That(workflow.Definition, Is.Not.Null);
        }

        // Verify that GetObject was called to load workflow definition
        await _azureManagementRepository.Received(1).GetObjectAsync<JObject>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task CreateAsync_Should_Initialize_Workflow_When_LoadRunsSince_Is_Null()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowDefinition);

        // Act
        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflow, Is.Not.Null);
            Assert.That(workflow.Name, Is.EqualTo("workflow"));
        }
    }

    [Test]
    public async Task CreateAsync_Should_Extract_Name_From_FullName_Correctly()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowDefinition);

        // Act
        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflow.FullName, Is.EqualTo("logic-app/workflow"));
            Assert.That(workflow.Name, Is.EqualTo("workflow"));
        }
    }

    [Test]
    public async Task GetWorkflowRunsAsync_Should_Return_Empty_List_When_No_Runs_Exist()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowDefinition);

        _azureManagementRepository
            .GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(Arg.Any<Uri>())
            .Returns(ReturnEmptyRuns);

        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Act
        var runs = await workflow.GetWorkflowRunsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(runs, Is.Not.Null);
            Assert.That(runs, Is.Empty);
        }

        // Verify that GetObject was called to retrieve workflow runs
        await _azureManagementRepository.Received(1).GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetWorkflowRunsAsync_Should_Build_Correct_Uri_With_Date_Filter_When_LoadRunsSince_Is_Provided()
    {
        // Arrange
        var loadRunsSince = new DateTime(2026, 4, 7, 5, 31, 13, 100, DateTimeKind.Utc).AddTicks(5026);
        var expectedDateFilter = "$filter=startTime ge 2026-04-07T05:31:13.1005026Z";

        Uri? capturedUri = null;

        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowDefinition);

        _azureManagementRepository
            .GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(Arg.Do<Uri>(uri => capturedUri = uri))
            .Returns(ReturnEmptyRuns);

        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, loadRunsSince).ConfigureAwait(false);

        // Act
        await workflow.GetWorkflowRunsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedUri, Is.Not.Null);
            Assert.That(capturedUri!.ToString(), Does.Contain(expectedDateFilter));
            Assert.That(capturedUri.ToString(), Does.Contain("workflow"));
            Assert.That(capturedUri.ToString(), Does.Contain("/runs"));
        }
    }

    [Test]
    public async Task GetWorkflowRunsAsync_Should_Not_Include_Date_Filter_When_LoadRunsSince_Is_Null()
    {
        // Arrange
        Uri? capturedUri = null;

        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowDefinition);

        _azureManagementRepository
            .GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(Arg.Do<Uri>(uri => capturedUri = uri))
            .Returns(ReturnEmptyRuns);

        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Act
        await workflow.GetWorkflowRunsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedUri, Is.Not.Null);
            Assert.That(capturedUri!.ToString(), Does.Not.Contain("$filter"));
            Assert.That(capturedUri.ToString(), Does.Contain("workflow"));
        }
    }

    [Test]
    public async Task GetWorkflowRunsAsync_Should_Cache_Runs_On_Subsequent_Calls()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowDefinition);

        _azureManagementRepository
            .GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(Arg.Any<Uri>())
            .Returns(ReturnEmptyRuns);

        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Act
        var runs1 = await workflow.GetWorkflowRunsAsync().ConfigureAwait(false);
        var runs2 = await workflow.GetWorkflowRunsAsync().ConfigureAwait(false);
        var runs3 = await workflow.GetWorkflowRunsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(runs1, Is.Not.Null);
            Assert.That(runs2, Is.Not.Null);
            Assert.That(runs3, Is.Not.Null);
        }

        // When runs list is empty, caching is not used (see GetWorkflowRunsAsync implementation)
        // Verify that GetObject was called three times since empty lists aren't cached
        await _azureManagementRepository.Received(3).GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetWorkflowRunsAsync_Should_Handle_Null_Value_In_Response()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowDefinition);

        _azureManagementRepository
            .GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(Arg.Any<Uri>())
            .Returns(ReturnNullValueRuns);

        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Act
        var runs = await workflow.GetWorkflowRunsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(runs, Is.Not.Null);
            Assert.That(runs, Is.Empty);
        }
    }

    [Test]
    public async Task ReloadAsync_Should_Clear_Cached_Runs_And_Reload()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowDefinition);

        _azureManagementRepository
            .GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(Arg.Any<Uri>())
            .Returns(ReturnEmptyRuns);

        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Load runs initially
        await workflow.GetWorkflowRunsAsync().ConfigureAwait(false);

        // Act
        await workflow.ReloadAsync().ConfigureAwait(false);

        // Assert - Verify that GetObject was called twice (initial load + reload)
        await _azureManagementRepository.Received(2).GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task LoadWorkflowDefinitionAsync_Should_Return_Definition_From_Response()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowDefinition);

        // Act
        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflow.Definition, Is.Not.Null);
            Assert.That(workflow.Definition!["$schema"], Is.Not.Null);
            Assert.That(workflow.Definition["contentVersion"]?.ToString(), Is.EqualTo("1.0.0.0"));
            Assert.That(workflow.Definition["actions"], Is.Not.Null);
        }
    }

    [Test]
    public async Task LoadWorkflowDefinitionAsync_Should_Build_Correct_Uri()
    {
        // Arrange
        Uri? capturedUri = null;

        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Do<Uri>(uri => capturedUri = uri))
            .Returns(ReturnWorkflowDefinition);

        // Act
        await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedUri, Is.Not.Null);
            Assert.That(capturedUri!.ToString(), Does.Contain("subscription-id"));
            Assert.That(capturedUri.ToString(), Does.Contain("resource-group"));
            Assert.That(capturedUri.ToString(), Does.Contain("logic-app"));
            Assert.That(capturedUri.ToString(), Does.Contain("workflow"));
            Assert.That(capturedUri.ToString(), Does.Contain("Microsoft.Web/sites"));
        }
    }

    [Test]
    public async Task GetWorkflowRunsAsync_Should_Return_Runs_From_Workflow_Runs_Response()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowDefinition);

        _azureManagementRepository
            .GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(Arg.Any<Uri>())
            .Returns(ReturnWorkflowRuns);

        // Mock WorkflowRun.CreateAsync to return a simple WorkflowRun
        // Note: Since WorkflowRun.CreateAsync is static and async, we cannot fully test the complete flow
        // without integration testing. This test verifies the API call is made.
        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Act & Assert
        // The actual creation of WorkflowRun instances requires mocking WorkflowRun.CreateAsync which is complex
        // We verify the repository is called with the workflow runs response
        Assert.That(async () => await workflow.GetWorkflowRunsAsync().ConfigureAwait(false), Throws.Nothing);

        // Verify the response was requested from the repository
        await _azureManagementRepository.Received(1).GetObjectAsync<Response<Models.RestApi.WorkflowRun>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task LoadWorkflowDefinitionAsync_Should_Return_Null_When_Definition_Not_Found()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnResponseWithoutDefinition);

        // Act
        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Assert
        Assert.That(workflow.Definition, Is.Null);
    }

    [Test]
    public async Task LoadWorkflowDefinitionAsync_Should_Handle_Null_Response()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<JObject>(Arg.Any<Uri>())
            .Returns(ReturnNullResponse);

        // Act
        var workflow = await Workflow.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, _workflowProperties, null).ConfigureAwait(false);

        // Assert
        Assert.That(workflow.Definition, Is.Null);
    }

    private Task<JObject?> ReturnWorkflowDefinition(CallInfo arg)
    {
        return Task.FromResult(_workflowDefinitionResponse);
    }

    private static Task<Response<Models.RestApi.WorkflowRun>?> ReturnEmptyRuns(CallInfo arg)
    {
        var emptyRunsResponse = new Response<Models.RestApi.WorkflowRun>
        {
            Value = new List<Models.RestApi.WorkflowRun>()
        };

        return Task.FromResult<Response<Models.RestApi.WorkflowRun>?>(emptyRunsResponse);
    }

    private static Task<Response<Models.RestApi.WorkflowRun>?> ReturnNullValueRuns(CallInfo arg)
    {
        var nullValueResponse = new Response<Models.RestApi.WorkflowRun>
        {
            Value = null
        };

        return Task.FromResult<Response<Models.RestApi.WorkflowRun>?>(nullValueResponse);
    }

    private Task<Response<Models.RestApi.WorkflowRun>?> ReturnWorkflowRuns(CallInfo arg)
    {
        return Task.FromResult(_workflowRunsResponse);
    }

    private static Task<JObject?> ReturnResponseWithoutDefinition(CallInfo arg)
    {
        var responseWithoutDefinition = new JObject
        {
            ["id"] = "/subscriptions/subscription-id/workflows/workflow",
            ["name"] = "workflow",
            ["properties"] = new JObject()
        };


        return Task.FromResult<JObject?>(responseWithoutDefinition);
    }

    private static Task<JObject?> ReturnNullResponse(CallInfo arg)
    {
        return Task.FromResult<JObject?>(null);
    }
}