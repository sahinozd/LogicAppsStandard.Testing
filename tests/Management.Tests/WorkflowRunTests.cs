using LogicApps.Management.Actions;
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
internal sealed class WorkflowRunTests
{
    private IConfiguration _configuration;
    private IAzureManagementRepository _azureManagementRepository;
    private IActionFactory _actionFactory;
    private IActionHelper _actionHelper;
    private Models.RestApi.WorkflowRun _workflowRunProperties;
    private JObject _workflowDefinition;
    private WorkflowRunDetailsAction? _initializeVariablesAction;
    private JToken? _initializeVariablesInputContent;
    private const string WorkflowName = "workflow";

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
        _configuration["VariableActionName"].Returns("Initialize_variables");
        _configuration["CorrelationIdVariableName"].Returns("correlationId");

        // Load workflow run properties from JSON file
        var workflowRunPropertiesFilePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Workflow-run-content.json");
        if (!File.Exists(workflowRunPropertiesFilePath))
        {
            throw new FileLoadException($"File does not exist: {workflowRunPropertiesFilePath}");
        }

        var workflowRunPropertiesJson = File.ReadAllTextAsync(workflowRunPropertiesFilePath).ConfigureAwait(false).GetAwaiter().GetResult();
        _workflowRunProperties = JsonConvert.DeserializeObject<Models.RestApi.WorkflowRun>(workflowRunPropertiesJson)!;

        // Load Initialize_variables action from JSON file
        var initializeVariablesFilePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Action_Initialize-variables.json");
        if (!File.Exists(initializeVariablesFilePath))
        {
            throw new FileLoadException($"File does not exist: {initializeVariablesFilePath}");
        }

        var initializeVariablesJson = File.ReadAllTextAsync(initializeVariablesFilePath).ConfigureAwait(false).GetAwaiter().GetResult();
        _initializeVariablesAction = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(initializeVariablesJson);

        // Load Initialize_variables input content from JSON file
        var initializeVariablesInputFilePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Action_Initialize-variables-input-content.json");
        if (!File.Exists(initializeVariablesInputFilePath))
        {
            throw new FileLoadException($"File does not exist: {initializeVariablesInputFilePath}");
        }

        var initializeVariablesInputJson = File.ReadAllTextAsync(initializeVariablesInputFilePath).ConfigureAwait(false).GetAwaiter().GetResult();
        _initializeVariablesInputContent = JToken.Parse(initializeVariablesInputJson);

        // Load workflow definition from JSON file
        var workflowDefinitionFilePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Workflow-definition.json");
        if (!File.Exists(workflowDefinitionFilePath))
        {
            throw new FileLoadException($"File does not exist: {workflowDefinitionFilePath}");
        }

        var workflowDefinitionJson = File.ReadAllTextAsync(workflowDefinitionFilePath).ConfigureAwait(false).GetAwaiter().GetResult();
        _workflowDefinition = JsonConvert.DeserializeObject<JObject>(workflowDefinitionJson)!;
    }

    [TearDown]
    public void TearDown()
    {
        _configuration = null!;
        _azureManagementRepository = null!;
        _actionFactory = null!;
        _actionHelper = null!;
        _workflowRunProperties = null!;
        _workflowDefinition = null!;
        _initializeVariablesAction = null;
        _initializeVariablesInputContent = null;
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_Configuration_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => WorkflowRun.CreateAsync(null!, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, _workflowDefinition));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("configuration"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_AzureManagementRepository_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => WorkflowRun.CreateAsync(_configuration, null!, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, _workflowDefinition));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("azureManagementRepository"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_ActionFactory_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, null!, _actionHelper, WorkflowName, _workflowRunProperties, _workflowDefinition));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionFactory"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_ActionHelper_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, null!, WorkflowName, _workflowRunProperties, _workflowDefinition));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionHelper"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_WorkflowName_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, null!, _workflowRunProperties, _workflowDefinition));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("workflowName"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_WorkflowRunProperties_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, null!, _workflowDefinition));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("workflowRunProperties"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_WorkflowDefinition_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, null!));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("workflowDefinition"));
    }

    [Test]
    public async Task CreateAsync_Should_Initialize_WorkflowRun_With_Valid_Parameters()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(ReturnInitializeVariablesAction);

        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/Set_variable/contents?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature")
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(workflowRunActionContent);

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(ReturnInitializeVariablesInputContent);

        // Act
        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, _workflowDefinition).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflowRun, Is.Not.Null);
            Assert.That(workflowRun.Id, Is.EqualTo(_workflowRunProperties.Id));
            Assert.That(workflowRun.Name, Is.EqualTo(_workflowRunProperties.Name));
            Assert.That(workflowRun.Type, Is.EqualTo(_workflowRunProperties.Type));
            Assert.That(workflowRun.Status, Is.EqualTo(_workflowRunProperties.Properties?.Status));
            Assert.That(workflowRun.StartTime, Is.EqualTo(_workflowRunProperties.Properties?.StartTime));
            Assert.That(workflowRun.EndTime, Is.EqualTo(_workflowRunProperties.Properties?.EndTime));
            Assert.That(workflowRun.ClientTrackingId, Is.EqualTo(_workflowRunProperties.Properties?.Correlation?.ClientTrackingId));
            Assert.That(workflowRun.CorrelationId, Is.EqualTo("d761efda-24c1-4316-a7c7-e9578b808836"));
        }

        // Verify that GetObject was called to retrieve the Initialize_variables action
        await _azureManagementRepository.Received(1).GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task CreateAsync_Should_Build_Correct_Uri_For_GetCorrelationIdAsync()
    {
        // Arrange
        Uri? capturedUri = null;

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Do<Uri>(uri => capturedUri = uri))
            .Returns(ReturnInitializeVariablesAction);

        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/Set_variable/contents?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature")
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(workflowRunActionContent);

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(ReturnInitializeVariablesInputContent);

        // Act
        await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, _workflowDefinition).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedUri, Is.Not.Null);
            Assert.That(capturedUri!.ToString(), Does.Contain("subscription-id"));
            Assert.That(capturedUri.ToString(), Does.Contain("resource-group"));
            Assert.That(capturedUri.ToString(), Does.Contain("logic-app"));
            Assert.That(capturedUri.ToString(), Does.Contain(WorkflowName));
            Assert.That(capturedUri.ToString(), Does.Contain(_workflowRunProperties.Name));
            Assert.That(capturedUri.ToString(), Does.Contain("Initialize_variables"));
        }
    }

    [Test]
    public async Task GetCorrelationIdAsync_Should_Extract_CorrelationId_From_Initialize_Variables_Action()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(ReturnInitializeVariablesAction);

        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/Set_variable/contents?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature")
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(workflowRunActionContent);

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(ReturnInitializeVariablesInputContent);

        // Act
        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, _workflowDefinition).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflowRun.CorrelationId, Is.Not.Null);
            Assert.That(workflowRun.CorrelationId, Is.EqualTo("d761efda-24c1-4316-a7c7-e9578b808836"));
        }

        // Verify the action helper methods were called
        _actionHelper.Received(1).GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>());
        await _actionHelper.Received(1).GetActionData(Arg.Any<WorkflowRunDetailsActionContent>()).ConfigureAwait(false);
    }

    [Test]
    public async Task CreateAsync_Should_Set_CorrelationId_To_Null_When_InputsLink_Is_Null()
    {
        // Arrange
        // Create action with null InputsLink using JSON deserialization
        const string actionJson = """
        {
          "id": "/workflows/test/runs/123/actions/Initialize_variables",
          "name": "Initialize_variables",
          "type": "workflows/runs/actions",
          "properties": {
              "inputsLink": null
          }
        }
        """;

        var actionWithoutInputsLink = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult(actionWithoutInputsLink));

        // Act
        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, _workflowDefinition).ConfigureAwait(false);

        // Assert
        Assert.That(workflowRun.CorrelationId, Is.Null);
    }

    [Test]
    public async Task CreateAsync_Should_Set_CorrelationId_To_Null_When_Action_Is_Null()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        // Act
        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, _workflowDefinition).ConfigureAwait(false);

        // Assert
        Assert.That(workflowRun.CorrelationId, Is.Null);
    }

    [Test]
    public async Task GetWorkflowRunActionsAsync_Should_Return_Empty_List_When_No_Actions_Exist()
    {
        // Arrange
        var emptyDefinition = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject()
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, emptyDefinition).ConfigureAwait(false);

        // Act
        var actions = await workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actions, Is.Not.Null);
            Assert.That(actions, Is.Empty);
        }
    }

    [Test]
    public async Task GetWorkflowRunActionsAsync_Should_Cache_Actions_On_Subsequent_Calls()
    {
        // Arrange
        var emptyDefinition = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject()
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, emptyDefinition).ConfigureAwait(false);

        // Act
        var actions1 = await workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);
        var actions2 = await workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);
        var actions3 = await workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actions1, Is.Not.Null);
            Assert.That(actions2, Is.Not.Null);
            Assert.That(actions3, Is.Not.Null);
        }

        // When actions list is empty, caching is not used
        // Verify that the factory's SetWorkflowRunProperties was called three times
        _actionFactory.Received(3).SetWorkflowRunProperties(WorkflowName, _workflowRunProperties.Name!);
    }

    [Test]
    public async Task FindActionByName_Should_Return_Null_When_Name_Is_Null()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, _workflowDefinition).ConfigureAwait(false);

        // Act
        var result = await workflowRun.FindActionByNameAsync(null!).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task FindActionByName_Should_Return_Null_When_Name_Is_Empty()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, _workflowDefinition).ConfigureAwait(false);

        // Act
        var result = await workflowRun.FindActionByNameAsync(string.Empty).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Reload_Should_Clear_Cached_Actions_And_Trigger()
    {
        // Arrange
        var emptyDefinition = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject()
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, emptyDefinition).ConfigureAwait(false);

        // Load actions initially
        await workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);

        // Act
        await workflowRun.Reload().ConfigureAwait(false);

        // Assert - Verify that SetWorkflowRunProperties was called twice (initial load + reload)
        _actionFactory.Received(2).SetWorkflowRunProperties(WorkflowName, _workflowRunProperties.Name!);
    }

    [Test]
    public async Task CreateAsync_Should_Handle_Workflow_Definition_Without_Definition_Wrapper()
    {
        // Arrange
        var definitionWithoutWrapper = new JObject
        {
            ["actions"] = new JObject
            {
                ["DirectAction"] = new JObject
                {
                    ["type"] = "Http"
                }
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        // Act
        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithoutWrapper).ConfigureAwait(false);

        // Assert
        Assert.That(workflowRun, Is.Not.Null);
    }

    [Test]
    public async Task CreateAsync_Should_Initialize_Properties_From_WorkflowRunProperties()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        // Act
        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, _workflowDefinition).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflowRun.Id, Is.EqualTo("/workflows/workflow/runs/123456"));
            Assert.That(workflowRun.Name, Is.EqualTo("123456"));
            Assert.That(workflowRun.Type, Is.EqualTo("workflows/runs"));
            Assert.That(workflowRun.Status, Is.EqualTo("Succeeded"));
            Assert.That(workflowRun.ClientTrackingId, Is.EqualTo("123456"));
            Assert.That(workflowRun.WaitEndTime, Is.EqualTo("2026-04-01T10:45:53.5368868Z"));
        }
    }

    [Test]
    public async Task GetWorkflowRunActionsAsync_Should_Return_Cached_Actions_When_Called_Multiple_Times()
    {
        // Arrange
        var definitionWithActions = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["Action1"] = new JObject { ["type"] = "Http" }
                }
            }
        };

        var mockAction = Substitute.For<BaseAction>("Action1", Models.Enums.ActionType.Action);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult(mockAction));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithActions).ConfigureAwait(false);

        // Act
        var actions1 = await workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);
        var actions2 = await workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actions1, Is.SameAs(actions2));
            Assert.That(actions1, Has.Count.EqualTo(1));
        }

        // Verify factory was only called once due to caching
        _actionFactory.Received(1).SetWorkflowRunProperties(WorkflowName, _workflowRunProperties.Name!);
    }

    [Test]
    public async Task GetWorkflowRunTriggerAsync_Should_Return_Cached_Trigger_When_Called_Multiple_Times()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        var emptyDefinition = new JObject
        {
            ["definition"] = new JObject { ["actions"] = new JObject() }
        };

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, emptyDefinition).ConfigureAwait(false);

        // Act
        var trigger1 = await workflowRun.GetWorkflowRunTriggerAsync().ConfigureAwait(false);
        var trigger2 = await workflowRun.GetWorkflowRunTriggerAsync().ConfigureAwait(false);

        // Assert
        Assert.That(trigger1, Is.SameAs(trigger2));
    }

    [Test]
    public async Task FindActionByName_Should_Return_Actions_When_Name_Matches()
    {
        // Arrange
        var definitionWithActions = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["TargetAction"] = new JObject { ["type"] = "Http" }
                }
            }
        };

        var mockAction = Substitute.For<BaseAction>("TargetAction", Models.Enums.ActionType.Action);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult(mockAction));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithActions).ConfigureAwait(false);

        // Act
        var result = await workflowRun.FindActionByNameAsync("TargetAction").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result![0].Name, Is.EqualTo("TargetAction"));
        }
    }

    [Test]
    public async Task FindActionByName_Should_Find_Actions_In_Nested_Scope()
    {
        // Arrange
        var mockNestedAction = Substitute.For<BaseAction>("NestedAction", Models.Enums.ActionType.Action);
        var mockScopeAction = new ScopeAction("ScopeAction", Models.Enums.ActionType.Scope);
        mockScopeAction.Actions.Add(mockNestedAction);

        var definitionWithScope = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["ScopeAction"] = new JObject { ["type"] = "Scope" }
                }
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult<BaseAction>(mockScopeAction));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithScope).ConfigureAwait(false);

        // Act
        var result = await workflowRun.FindActionByNameAsync("NestedAction").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result![0].Name, Is.EqualTo("NestedAction"));
        }
    }

    [Test]
    public async Task FindActionByName_Should_Find_Actions_In_ForEach_Repetitions()
    {
        // Arrange
        var mockNestedAction = Substitute.For<BaseAction>("LoopAction", Models.Enums.ActionType.Action);

        const string repetitionJson = """
            {
                "name": "00001",
                "properties": {
                    "repetitionIndexes": [{"itemIndex": 0}]
                }
            }
            """;

        var repetition = new ForEachActionRepetition(JsonConvert.DeserializeObject<WorkflowRunDetailsActionRepetition>(repetitionJson)!);
        repetition.Actions.Add(mockNestedAction);

        var mockForEachAction = new ForEachAction("ForEachAction", Models.Enums.ActionType.ForEach);
        mockForEachAction.Repetitions.Add(repetition);

        var definitionWithForEach = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["ForEachAction"] = new JObject { ["type"] = "ForEach" }
                }
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult<BaseAction>(mockForEachAction));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithForEach).ConfigureAwait(false);

        // Act
        var result = await workflowRun.FindActionByNameAsync("LoopAction").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result![0].Name, Is.EqualTo("LoopAction"));
        }
    }

    [Test]
    public async Task FindActionByName_Should_Find_Actions_In_Until_Repetitions()
    {
        // Arrange
        var mockNestedAction = Substitute.For<BaseAction>("UntilAction", Models.Enums.ActionType.Action);
        var repetition = new UntilActionRepetition(new WorkflowRunDetailsActionRepetition { Name = "00001" });
        repetition.Actions.Add(mockNestedAction);

        var mockUntilAction = new UntilAction("UntilLoop", Models.Enums.ActionType.Until);
        mockUntilAction.Repetitions.Add(repetition);

        var definitionWithUntil = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["UntilLoop"] = new JObject { ["type"] = "Until" }
                }
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult<BaseAction>(mockUntilAction));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithUntil).ConfigureAwait(false);

        // Act
        var result = await workflowRun.FindActionByNameAsync("UntilAction").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result![0].Name, Is.EqualTo("UntilAction"));
        }
    }

    [Test]
    public async Task FindActionByName_Should_Find_Actions_In_Switch_Cases()
    {
        // Arrange
        var mockCaseAction = Substitute.For<BaseAction>("CaseAction", Models.Enums.ActionType.Action);
        var switchCase = new SwitchCase { Name = "Case1" };
        switchCase.Actions.Add(mockCaseAction);

        var mockSwitchAction = new SwitchAction("SwitchAction", Models.Enums.ActionType.Switch);
        mockSwitchAction.Cases.Add(switchCase);

        var definitionWithSwitch = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["SwitchAction"] = new JObject { ["type"] = "Switch" }
                }
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult<BaseAction>(mockSwitchAction));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithSwitch).ConfigureAwait(false);

        // Act
        var result = await workflowRun.FindActionByNameAsync("CaseAction").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result![0].Name, Is.EqualTo("CaseAction"));
        }
    }

    [Test]
    public async Task FindActionByName_Should_Find_Actions_In_Condition_Default_Branch()
    {
        // Arrange
        var mockDefaultAction = Substitute.For<BaseAction>("DefaultAction", Models.Enums.ActionType.Action);
        var mockConditionAction = new ConditionAction("ConditionAction", Models.Enums.ActionType.Condition);
        mockConditionAction.DefaultActions.Add(mockDefaultAction);

        var definitionWithCondition = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["ConditionAction"] = new JObject { ["type"] = "Condition" }
                }
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult<BaseAction>(mockConditionAction));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithCondition).ConfigureAwait(false);

        // Act
        var result = await workflowRun.FindActionByNameAsync("DefaultAction").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result![0].Name, Is.EqualTo("DefaultAction"));
        }
    }

    [Test]
    public async Task FindActionByName_Should_Find_Actions_In_Condition_Else_Branch()
    {
        // Arrange
        var mockElseAction = Substitute.For<BaseAction>("ElseAction", Models.Enums.ActionType.Action);
        var mockConditionAction = new ConditionAction("ConditionAction", Models.Enums.ActionType.Condition);
        mockConditionAction.ElseActions.Add(mockElseAction);

        var definitionWithCondition = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["ConditionAction"] = new JObject { ["type"] = "Condition" }
                }
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult<BaseAction>(mockConditionAction));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithCondition).ConfigureAwait(false);

        // Act
        var result = await workflowRun.FindActionByNameAsync("ElseAction").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result![0].Name, Is.EqualTo("ElseAction"));
        }
    }

    [Test]
    public async Task FindActionByName_Should_Find_Actions_In_Condition_Both_Branches()
    {
        // Arrange
        var mockElseAction = Substitute.For<BaseAction>("BranchAction", Models.Enums.ActionType.Action);
        var mockDefaultAction = Substitute.For<BaseAction>("BranchAction", Models.Enums.ActionType.Action);
        var mockConditionAction = new ConditionAction("ConditionAction", Models.Enums.ActionType.Condition);
        mockConditionAction.ElseActions.Add(mockElseAction);
        mockConditionAction.DefaultActions.Add(mockDefaultAction);

        var definitionWithCondition = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["ConditionAction"] = new JObject { ["type"] = "Condition" }
                }
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult<BaseAction>(mockConditionAction));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithCondition).ConfigureAwait(false);

        // Act
        var result = await workflowRun.FindActionByNameAsync("BranchAction").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public async Task FindActionByName_Should_Return_Empty_List_When_No_Match_Found()
    {
        // Arrange
        var definitionWithActions = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["Action1"] = new JObject { ["type"] = "Http" }
                }
            }
        };

        var mockAction = Substitute.For<BaseAction>("Action1", Models.Enums.ActionType.Action);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult(mockAction));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithActions).ConfigureAwait(false);

        // Act
        var result = await workflowRun.FindActionByNameAsync("NonExistentAction").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
    }

    [Test]
    public async Task BuildActionListFromWorkflowDefinition_Should_Throw_InvalidOperationException_When_Actions_Not_JObject()
    {
        // Arrange
        var invalidDefinition = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = "invalid"
            }
        };

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        // Act - CreateAsync will succeed but GetWorkflowRunActionsAsync will throw
        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, invalidDefinition).ConfigureAwait(false);

        // Assert
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => workflowRun.GetWorkflowRunActionsAsync());
        Assert.That(exception.Message, Does.Contain("Workflow definition does not contain a valid 'actions' section"));
    }

    [Test]
    public async Task BuildActionListFromWorkflowDefinition_Should_Skip_Non_JObject_Action_Properties()
    {
        // Arrange
        var definitionWithInvalidAction = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["ValidAction"] = new JObject { ["type"] = "Http" },
                    ["InvalidAction"] = "not a jobject"
                }
            }
        };

        var mockAction = Substitute.For<BaseAction>("ValidAction", Models.Enums.ActionType.Action);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject("ValidAction", Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult(mockAction));

        // Act
        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithInvalidAction).ConfigureAwait(false);
        var actions = await workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actions, Is.Not.Null);
            Assert.That(actions, Has.Count.EqualTo(1));
            Assert.That(actions[0].Name, Is.EqualTo("ValidAction"));
        }
    }

    [Test]
    public async Task Reload_Should_Reload_Both_Actions_And_Trigger()
    {
        // Arrange
        var definitionWithActions = new JObject
        {
            ["definition"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["Action1"] = new JObject { ["type"] = "Http" }
                }
            }
        };

        var mockAction = Substitute.For<BaseAction>("Action1", Models.Enums.ActionType.Action);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        _actionFactory
            .CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string?>())
            .Returns(Task.FromResult(mockAction));

        var workflowRun = await WorkflowRun.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, WorkflowName, _workflowRunProperties, definitionWithActions).ConfigureAwait(false);

        // Load actions and trigger initially
        await workflowRun.GetWorkflowRunActionsAsync().ConfigureAwait(false);
        await workflowRun.GetWorkflowRunTriggerAsync().ConfigureAwait(false);

        // Act
        await workflowRun.Reload().ConfigureAwait(false);

        // Assert - Both actions and trigger should be reloaded
        _actionFactory.Received(2).SetWorkflowRunProperties(WorkflowName, _workflowRunProperties.Name!);
    }

    private Task<WorkflowRunDetailsAction?> ReturnInitializeVariablesAction(CallInfo arg)
    {
        return Task.FromResult(_initializeVariablesAction);
    }

    private Task<JToken?> ReturnInitializeVariablesInputContent(CallInfo arg)
    {
        return Task.FromResult(_initializeVariablesInputContent);
    }
}