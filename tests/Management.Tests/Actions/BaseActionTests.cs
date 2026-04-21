using LogicApps.Management.Helper;
using LogicApps.Management.Models.Enums;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;

namespace LogicApps.Management.Tests.Actions;

[TestFixture]
internal sealed class BaseActionTests
{
    private IConfiguration _configuration;
    private IAzureManagementRepository _azureManagementRepository;
    private IActionHelper _actionHelper;
    private WorkflowRunDetailsAction? _actionDetailsResponse;
    private Response<WorkflowRunDetailsAction>? _actionRepetitionsResponse;
    private JToken? _actionInputContent;

    [SetUp]
    public void SetUp()
    {
        // Initialize mocked dependencies
        _configuration = Substitute.For<IConfiguration>();
        _azureManagementRepository = Substitute.For<IAzureManagementRepository>();
        _actionHelper = Substitute.For<IActionHelper>();

        // Setup configuration values
        _configuration["SubscriptionId"].Returns("subscription-id");
        _configuration["ResourceGroup"].Returns("resource-group");
        _configuration["LogicAppName"].Returns("logic-app");
        _configuration["LogicAppApiVersion"].Returns("2025-05-01");

        // Create action details response manually with properties
        const string actionDetailsJson = 
             """
             {
                 "id": "/workflows/workflow/runs/run/actions/Set_variable",
                 "name": "Set_variable",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded",
                     "code": "OK",
                     "canResubmit": true
                 }
             }
             """;

        _actionDetailsResponse = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        // Create action repetitions response
        _actionRepetitionsResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = new List<WorkflowRunDetailsAction>
            {
                new()
                {
                    Id = "/workflows/test/runs/run123/actions/MockAction/repetitions/000001",
                    Name = "000001",
                    Type = "workflows/runs/actions/repetitions"
                }
            },
            NextLink = null
        };

        // Load action input content from JSON file
        var filePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Action_Set-variable-input-content.json");
        if (!File.Exists(filePath))
        {
            throw new FileLoadException($"File does not exist: {filePath}");
        }

        var actionInputJson = File.ReadAllTextAsync(filePath).ConfigureAwait(false).GetAwaiter().GetResult();
        _actionInputContent = JToken.Parse(actionInputJson);
    }

    [TearDown]
    public void TearDown()
    {
        _configuration = null!;
        _azureManagementRepository = null!;
        _actionHelper = null!;
        _actionDetailsResponse = null;
        _actionRepetitionsResponse = null;
        _actionInputContent = null;
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_With_Valid_Parameters()
    {
        // Arrange & Act
        var action = new MockAction("MockAction", ActionType.Action);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action.Name, Is.EqualTo("MockAction"));
            Assert.That(action.Type, Is.EqualTo(ActionType.Action));
        }
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentException_When_Name_Is_Null()
    {
        // Arrange & Act
        var argumentException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new MockAction(null!, ActionType.Action);
        });

        // Assert
        Assert.That(argumentException?.ParamName, Is.EqualTo("name"));
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentException_When_Name_Is_Empty()
    {
        // Arrange & Act
        var argumentException = Assert.Throws<ArgumentException>(() =>
        {
            _ = new MockAction(string.Empty, ActionType.Action);
        });

        // Assert
        Assert.That(argumentException?.ParamName, Is.EqualTo("name"));
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentOutOfRangeException_When_ActionType_Is_Invalid()
    {
        // Arrange & Act
        var argumentOutOfRangeException = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new MockAction("MockAction", (ActionType)999);
        });

        // Assert
        Assert.That(argumentOutOfRangeException?.ParamName, Is.EqualTo("actionType"));
    }

    [Test]
    public void LoadActionDetails_Should_Throw_ArgumentNullException_When_Configuration_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => action.LoadActionDetails(null!, _azureManagementRepository, _actionHelper, "workflow", "run123"));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("configuration"));
    }

    [Test]
    public void LoadActionDetails_Should_Throw_ArgumentNullException_When_AzureManagementRepository_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => action.LoadActionDetails(_configuration, null!, _actionHelper, "workflow", "run123"));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("azureManagementRepository"));
    }

    [Test]
    public void LoadActionDetails_Should_Throw_ArgumentNullException_When_ActionHelper_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => action.LoadActionDetails(_configuration, _azureManagementRepository, null!, "workflow", "run123"));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionHelper"));
    }

    [Test]
    public void LoadActionDetails_Should_Throw_ArgumentException_When_WorkflowName_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentNullException>(() => action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, null!, "run123"));

        Assert.That(argumentException.ParamName, Is.EqualTo("workflowName"));
    }

    [Test]
    public void LoadActionDetails_Should_Throw_ArgumentException_When_WorkflowName_Is_Empty()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentException>(() => action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, string.Empty, "run123"));

        Assert.That(argumentException.ParamName, Is.EqualTo("workflowName"));
    }

    [Test]
    public void LoadActionDetails_Should_Throw_ArgumentException_When_RunId_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentNullException>(() => action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", null!));

        Assert.That(argumentException.ParamName, Is.EqualTo("runId"));
    }

    [Test]
    public void LoadActionDetails_Should_Throw_ArgumentException_When_RunId_Is_Empty()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentException>(() => action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", string.Empty));

        Assert.That(argumentException.ParamName, Is.EqualTo("runId"));
    }

    [Test]
    public async Task LoadActionDetails_Should_Load_Properties_From_API_Response()
    {
        // Arrange
        var action = new MockAction("Set_variable", ActionType.Action);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(ReturnActionDetails);

        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/Set_variable/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature"),
            ContentSize = 100
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(workflowRunActionContent);

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(ReturnActionInputContent);

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.Id, Is.Not.Null);
            Assert.That(action.Status, Is.Not.Null);
            Assert.That(action.Code, Is.Not.Null);
            Assert.That(action.DesignerName, Is.EqualTo("Set variable"));
            Assert.That(action.CanResubmit, Is.Not.Null);
        }

        // Verify API was called
        await _azureManagementRepository.Received(1).GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task LoadActionDetails_Should_Build_Correct_Uri()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);
        Uri? capturedUri = null;

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Do<Uri>(uri => capturedUri = uri))
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedUri, Is.Not.Null);
            Assert.That(capturedUri!.ToString(), Does.Contain("subscription-id"));
            Assert.That(capturedUri.ToString(), Does.Contain("resource-group"));
            Assert.That(capturedUri.ToString(), Does.Contain("logic-app"));
            Assert.That(capturedUri.ToString(), Does.Contain("workflow"));
            Assert.That(capturedUri.ToString(), Does.Contain("run123"));
            Assert.That(capturedUri.ToString(), Does.Contain("MockAction"));
        }
    }

    [Test]
    public async Task LoadActionDetails_Should_Handle_Null_Response()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetailsAction?>(null));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert - Should not throw exception
        Assert.That(action.Id, Is.Null);
    }

    [Test]
    public void SetActionDetails_Should_Throw_ArgumentNullException_When_ActionHelper_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => action.SetActionDetails(null!, _actionDetailsResponse!));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionHelper"));
    }

    [Test]
    public void SetActionDetails_Should_Throw_ArgumentNullException_When_Result_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => action.SetActionDetails(_actionHelper, null!));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("result"));
    }

    [Test]
    public async Task SetActionDetails_Should_Set_Properties_From_Result()
    {
        // Arrange
        var action = new MockAction("Set_variable", ActionType.Action);

        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/Set_variable/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature"),
            ContentSize = 100
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(workflowRunActionContent);

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(Task.FromResult(_actionInputContent));

        // Act
        await action.SetActionDetails(_actionHelper, _actionDetailsResponse!).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.Id, Is.Not.Null);
            Assert.That(action.Status, Is.Not.Null);
            Assert.That(action.DesignerName, Is.EqualTo("Set variable"));
        }
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_Configuration_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => action.GetAllActionRepetitions(null!, _azureManagementRepository, _actionHelper, "workflow", "run123"));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("configuration"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_AzureManagementRepository_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => action.GetAllActionRepetitions(_configuration, null!, _actionHelper, "workflow", "run123"));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("azureManagementRepository"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_ActionHelper_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => action.GetAllActionRepetitions(_configuration, _azureManagementRepository, null!, "workflow", "run123"));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionHelper"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentException_When_WorkflowName_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentNullException>(() => action.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, null!, "run123"));

        Assert.That(argumentException.ParamName, Is.EqualTo("workflowName"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentException_When_WorkflowName_Is_Empty()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentException>(() => action.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, string.Empty, "run123"));

        Assert.That(argumentException.ParamName, Is.EqualTo("workflowName"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentException_When_RunId_Is_Null()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentNullException>(() => action.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", null!));

        Assert.That(argumentException.ParamName, Is.EqualTo("runId"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentException_When_RunId_Is_Empty()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentException>(() => action.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", string.Empty));

        Assert.That(argumentException.ParamName, Is.EqualTo("runId"));
    }

    [Test]
    public async Task GetAllActionRepetitions_Should_Return_Empty_List_When_No_Repetitions_Exist()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        var emptyResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = new List<WorkflowRunDetailsAction>(),
            NextLink = null
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(emptyResponse));

        // Act
        var result = await action.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
    }

    [Test]
    public async Task GetAllActionRepetitions_Should_Return_Repetitions_When_Single_Page_Exists()
    {
        // Arrange
        var action = new MockAction("ForEach_1", ActionType.ForEach);

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(ReturnActionRepetitions);

        // Act
        var result = await action.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.GreaterThan(0));
        }
    }

    [Test]
    public async Task GetAllActionRepetitions_Should_Follow_NextLink_When_Multiple_Pages_Exist()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);

        var repetition1 = new WorkflowRunDetailsAction
        {
            Id = "/workflows/test/runs/run123/actions/MockAction/repetitions/000001",
            Name = "000001",
            Type = "workflows/runs/actions/repetitions"
        };

        var repetition2 = new WorkflowRunDetailsAction
        {
            Id = "/workflows/test/runs/run123/actions/MockAction/repetitions/000002",
            Name = "000002",
            Type = "workflows/runs/actions/repetitions"
        };

        var firstPageResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = new List<WorkflowRunDetailsAction> { repetition1 },
            NextLink = "https://management.azure.com/subscriptions/subscription-id/resourceGroups/resource-group/providers/Microsoft.Web/sites/logicapp-name/hostruntime/runtime/webhooks/workflow/api/management/workflows/workflow-name/runs/wun-id/actions/MockAction/repetitions?api_version=2025-05-01&skipToken=token&code=key"
        };

        var secondPageResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = new List<WorkflowRunDetailsAction> { repetition2 },
            NextLink = null
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(firstPageResponse, secondPageResponse);

        // Act
        var result = await action.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("000001"));
            Assert.That(result[1].Name, Is.EqualTo("000002"));
        }

        // Verify two API calls were made
        await _azureManagementRepository.Received(2).GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetAllActionRepetitions_Should_Build_Correct_Uri()
    {
        // Arrange
        var action = new MockAction("MockAction", ActionType.Action);
        Uri? capturedUri = null;

        var emptyResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = new List<WorkflowRunDetailsAction>(),
            NextLink = null
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Do<Uri>(uri => capturedUri = uri))
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(emptyResponse));

        // Act
        await action.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedUri, Is.Not.Null);
            Assert.That(capturedUri!.ToString(), Does.Contain("subscription-id"));
            Assert.That(capturedUri.ToString(), Does.Contain("resource-group"));
            Assert.That(capturedUri.ToString(), Does.Contain("logic-app"));
            Assert.That(capturedUri.ToString(), Does.Contain("workflow"));
            Assert.That(capturedUri.ToString(), Does.Contain("run123"));
            Assert.That(capturedUri.ToString(), Does.Contain("MockAction"));
            Assert.That(capturedUri.ToString(), Does.Contain("repetitions"));
        }
    }

    [Test]
    public Task Properties_Should_Be_Settable()
    {
        // Arrange & Act
        var action = new MockAction("MockAction", ActionType.Action)
        {
            CanResubmit = true,
            Code = "OK",
            DesignerName = "Test Designer Name",
            EndTime = DateTime.UtcNow,
            Id = "/test/id",
            OriginHistoryName = "origin123",
            ScheduledTime = DateTime.UtcNow,
            StartTime = DateTime.UtcNow,
            Status = "Succeeded",
            TrackingId = "tracking123",
            IterationCount = 5,
            RepetitionCount = 10
        };

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.CanResubmit, Is.True);
            Assert.That(action.Code, Is.EqualTo("OK"));
            Assert.That(action.DesignerName, Is.EqualTo("Test Designer Name"));
            Assert.That(action.EndTime, Is.Not.Null);
            Assert.That(action.Id, Is.EqualTo("/test/id"));
            Assert.That(action.OriginHistoryName, Is.EqualTo("origin123"));
            Assert.That(action.ScheduledTime, Is.Not.Null);
            Assert.That(action.StartTime, Is.Not.Null);
            Assert.That(action.Status, Is.EqualTo("Succeeded"));
            Assert.That(action.TrackingId, Is.EqualTo("tracking123"));
            Assert.That(action.IterationCount, Is.EqualTo(5));
            Assert.That(action.RepetitionCount, Is.EqualTo(10));
        }

        return Task.CompletedTask;
    }

    [Test]
    public Task Properties_Should_Handle_Null_Values()
    {
        // Arrange & Act
        var action = new MockAction("MockAction", ActionType.Action)
        {
            Correlation = null,
            Error = null,
            InputsLink = null,
            OutputsLink = null,
            Input = null,
            Output = null
        };

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.Correlation, Is.Null);
            Assert.That(action.Error, Is.Null);
            Assert.That(action.InputsLink, Is.Null);
            Assert.That(action.OutputsLink, Is.Null);
            Assert.That(action.Input, Is.Null);
            Assert.That(action.Output, Is.Null);
        }

        return Task.CompletedTask;
    }

    [Test]
    public async Task LoadActionDetails_Should_Map_Correlation_When_Present()
    {
        // Arrange
        var action = new MockAction("TestAction", ActionType.Action);

        const string actionDetailsJson = 
            """
            {
                 "id": "/workflows/test/runs/run123/actions/TestAction",
                 "name": "TestAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded",
                     "correlation": {
                         "clientTrackingId": "client-123",
                         "actionTrackingId": "action-456"
                     }
                 }
            }
            """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult(actionDetails));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.Correlation, Is.Not.Null);
            Assert.That(action.Correlation!.ClientTrackingId, Is.EqualTo("client-123"));
            Assert.That(action.Correlation.ActionTrackingId, Is.EqualTo("action-456"));
        }
    }

    [Test]
    public async Task LoadActionDetails_Should_Map_Error_When_Present()
    {
        // Arrange
        var action = new MockAction("TestAction", ActionType.Action);

        const string actionDetailsJson = 
             """
             {
                 "id": "/workflows/test/runs/run123/actions/TestAction",
                 "name": "TestAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Failed",
                     "error": {
                         "code": "ActionFailed",
                         "message": "The action failed due to an error"
                     }
                 }
             }
             """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult(actionDetails));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.Error, Is.Not.Null);
            Assert.That(action.Error!.Code, Is.EqualTo("ActionFailed"));
            Assert.That(action.Error.Message, Is.EqualTo("The action failed due to an error"));
        }
    }

    [Test]
    public async Task LoadActionDetails_Should_Handle_Null_Correlation()
    {
        // Arrange
        var action = new MockAction("TestAction", ActionType.Action);

        const string actionDetailsJson = 
             """
             {
                 "id": "/workflows/test/runs/run123/actions/TestAction",
                 "name": "TestAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded",
                     "correlation": null
                 }
             }
             """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult(actionDetails));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        Assert.That(action.Correlation, Is.Null);
    }

    [Test]
    public async Task LoadActionDetails_Should_Handle_Null_Error()
    {
        // Arrange
        var action = new MockAction("TestAction", ActionType.Action);

        const string actionDetailsJson = 
            """
             {
                 "id": "/workflows/test/runs/run123/actions/TestAction",
                 "name": "TestAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded",
                     "error": null
                 }
             }
            """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult(actionDetails));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        Assert.That(action.Error, Is.Null);
    }

    [Test]
    public async Task LoadActionDetails_Should_Build_InputsLink_Content_With_Metadata()
    {
        // Arrange
        var action = new MockAction("TestAction", ActionType.Action);

        const string actionDetailsJson =
            """
             {
                 "id": "/workflows/test/runs/run123/actions/TestAction",
                 "name": "TestAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded",
                     "inputsLink": {
                         "uri": "https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature",
                         "contentSize": 256,
                         "metadata": {
                             "foreachItemsCount": 5
                         }
                     }
                 }
             }
            """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult(actionDetails));

        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature"),
            ContentSize = 256,
            Metadata = new WorkflowRunDetailsActionContentMetadata
            {
                ForeachItemsCount = 5
            }
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(workflowRunActionContent);

        var inputData = JToken.Parse("""{"testInput": "value"}""");

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(Task.FromResult<JToken?>(inputData));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.InputsLink, Is.Not.Null);
            Assert.That(action.InputsLink!.Uri, Is.EqualTo(new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature")));
            Assert.That(action.InputsLink.ContentSize, Is.EqualTo(256));
            Assert.That(action.InputsLink.Metadata, Is.Not.Null);
            Assert.That(action.InputsLink.Metadata!.ForeachItemsCount, Is.EqualTo(5));
            Assert.That(action.Input, Is.Not.Null);
            Assert.That(action.Input!["testInput"]?.ToString(), Is.EqualTo("value"));
        }
    }

    [Test]
    public async Task LoadActionDetails_Should_Build_OutputsLink_Content_With_Metadata()
    {
        // Arrange
        var action = new MockAction("TestAction", ActionType.Action);

        const string actionDetailsJson =
            """
             {
                 "id": "/workflows/test/runs/run123/actions/TestAction",
                 "name": "TestAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded",
                     "outputsLink": {
                         "uri": "https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature",
                         "contentSize": 512,
                         "metadata": {
                             "foreachItemsCount": 10
                         }
                     }
                 }
             }
            """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult(actionDetails));

        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature"),
            ContentSize = 512,
            Metadata = new WorkflowRunDetailsActionContentMetadata
            {
                ForeachItemsCount = 10
            }
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(workflowRunActionContent);

        var outputData = JToken.Parse("""{"testOutput": "result"}""");

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(Task.FromResult<JToken?>(outputData));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.OutputsLink, Is.Not.Null);
            Assert.That(action.OutputsLink!.Uri, Is.EqualTo(new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature")));
            Assert.That(action.OutputsLink.ContentSize, Is.EqualTo(512));
            Assert.That(action.OutputsLink.Metadata, Is.Not.Null);
            Assert.That(action.OutputsLink.Metadata!.ForeachItemsCount, Is.EqualTo(10));
            Assert.That(action.Output, Is.Not.Null);
            Assert.That(action.Output!["testOutput"]?.ToString(), Is.EqualTo("result"));
        }
    }

    [Test]
    public async Task LoadActionDetails_Should_Build_Content_Without_Metadata()
    {
        // Arrange
        var action = new MockAction("TestAction", ActionType.Action);

        const string actionDetailsJson =
            """
             {
                 "id": "/workflows/test/runs/run123/actions/TestAction",
                 "name": "TestAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded",
                     "inputsLink": {
                         "uri": "https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature",
                         "contentSize": 128,
                         "metadata": null
                     }
                 }
             }
             """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult(actionDetails));

        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature"),
            ContentSize = 128,
            Metadata = null
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(workflowRunActionContent);

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(Task.FromResult<JToken?>(null));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.InputsLink, Is.Not.Null);
            Assert.That(action.InputsLink!.ContentSize, Is.EqualTo(128));
            Assert.That(action.InputsLink.Metadata, Is.Not.Null);
            Assert.That(action.InputsLink.Metadata!.ForeachItemsCount, Is.Null);
        }
    }

    [Test]
    public async Task LoadActionDetails_Should_Handle_Null_InputsLink_And_OutputsLink()
    {
        // Arrange
        var action = new MockAction("TestAction", ActionType.Action);

        const string actionDetailsJson = 
            """
             {
                 "id": "/workflows/test/runs/run123/actions/TestAction",
                 "name": "TestAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded",
                     "inputsLink": null,
                     "outputsLink": null
                 }
             }
             """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult(actionDetails));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.InputsLink, Is.Null);
            Assert.That(action.OutputsLink, Is.Null);
            Assert.That(action.Input, Is.Null);
            Assert.That(action.Output, Is.Null);
        }
    }

    [Test]
    public async Task SetActionDetails_Should_Map_Correlation_When_Present()
    {
        // Arrange
        var action = new MockAction("TestAction", ActionType.Action);

        const string actionDetailsJson = 
            """
             {
                 "id": "/workflows/test/runs/run123/actions/TestAction",
                 "name": "TestAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded",
                     "correlation": {
                         "clientTrackingId": "client-tracking-789",
                         "actionTrackingId": "action-tracking-012"
                     }
                 }
             }
             """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        // Act
        await action.SetActionDetails(_actionHelper, actionDetails!).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.Correlation, Is.Not.Null);
            Assert.That(action.Correlation!.ClientTrackingId, Is.EqualTo("client-tracking-789"));
            Assert.That(action.Correlation.ActionTrackingId, Is.EqualTo("action-tracking-012"));
        }
    }

    [Test]
    public async Task SetActionDetails_Should_Map_Error_When_Present()
    {
        // Arrange
        var action = new MockAction("TestAction", ActionType.Action);

        const string actionDetailsJson = 
            """
             {
                 "id": "/workflows/test/runs/run123/actions/TestAction",
                 "name": "TestAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Failed",
                     "error": {
                         "code": "ServiceError",
                         "message": "Internal server error occurred"
                     }
                 }
             }
             """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        // Act
        await action.SetActionDetails(_actionHelper, actionDetails!).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.Error, Is.Not.Null);
            Assert.That(action.Error!.Code, Is.EqualTo("ServiceError"));
            Assert.That(action.Error.Message, Is.EqualTo("Internal server error occurred"));
        }
    }

    [Test]
    public async Task SetActionDetails_Should_Build_InputsLink_And_OutputsLink_Content()
    {
        // Arrange
        var action = new MockAction("TestAction", ActionType.Action);

        const string actionDetailsJson =
            """
             {
                 "id": "/workflows/test/runs/run123/actions/TestAction",
                 "name": "TestAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded",
                     "inputsLink": {
                         "uri": "https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature",
                         "contentSize": 1024,
                         "metadata": {
                             "foreachItemsCount": 3
                         }
                     },
                     "outputsLink": {
                         "uri": "https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature",
                         "contentSize": 2048,
                         "metadata": {
                             "foreachItemsCount": 7
                         }
                     }
                 }
             }
             """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        var inputContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature"),
            ContentSize = 1024,
            Metadata = new WorkflowRunDetailsActionContentMetadata
            {
                ForeachItemsCount = 3
            }
        };

        var outputContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature"),
            ContentSize = 2048,
            Metadata = new WorkflowRunDetailsActionContentMetadata
            {
                ForeachItemsCount = 7
            }
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(x => 
            {
                var input = x.Arg<WorkflowRunDetailsActionContent>();
                return input.Uri?.ToString().Contains("ActionInputs", StringComparison.Ordinal) == true ? inputContent : outputContent;
            });

        var inputData = JToken.Parse("""{"input": "data"}""");
        var outputData = JToken.Parse("""{"output": "result"}""");

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(x => 
            {
                var input = x.Arg<WorkflowRunDetailsActionContent>();
                return Task.FromResult<JToken?>(input.Uri?.ToString().Contains("ActionInputs", StringComparison.Ordinal) == true ? inputData : outputData);
            });

        // Act
        await action.SetActionDetails(_actionHelper, actionDetails!).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.InputsLink, Is.Not.Null);
            Assert.That(action.InputsLink!.Uri, Is.EqualTo(new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature")));
            Assert.That(action.InputsLink.ContentSize, Is.EqualTo(1024));
            Assert.That(action.InputsLink.Metadata, Is.Not.Null);
            Assert.That(action.InputsLink.Metadata!.ForeachItemsCount, Is.EqualTo(3));
            Assert.That(action.Input, Is.Not.Null);

            Assert.That(action.OutputsLink, Is.Not.Null);
            Assert.That(action.OutputsLink!.Uri, Is.EqualTo(new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature")));
            Assert.That(action.OutputsLink.ContentSize, Is.EqualTo(2048));
            Assert.That(action.OutputsLink.Metadata, Is.Not.Null);
            Assert.That(action.OutputsLink.Metadata!.ForeachItemsCount, Is.EqualTo(7));
            Assert.That(action.Output, Is.Not.Null);
        }

        // Verify GetWorkflowRunActionContent was called for both inputs and outputs
        _actionHelper.Received(2).GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>());
        await _actionHelper.Received(2).GetActionData(Arg.Any<WorkflowRunDetailsActionContent>()).ConfigureAwait(false);
    }

    [Test]
    public async Task LoadActionDetails_Should_Load_All_Properties_Including_Timing_And_Tracking()
    {
        // Arrange
        var action = new MockAction("CompleteAction", ActionType.Action);

        const string actionDetailsJson = 
            """
             {
                 "id": "/workflows/test/runs/run123/actions/CompleteAction",
                 "name": "CompleteAction",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded",
                     "code": "OK",
                     "canResubmit": true,
                     "startTime": "2024-01-15T10:30:00Z",
                     "endTime": "2024-01-15T10:30:05Z",
                     "scheduledTime": "2024-01-15T10:29:55Z",
                     "originHistoryName": "original-run-456",
                     "trackingId": "tracking-abc-123",
                     "iterationCount": 3,
                     "repetitionCount": 5,
                     "correlation": {
                         "clientTrackingId": "client-xyz",
                         "actionTrackingId": "action-xyz"
                     }
                 }
             }
             """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult(actionDetails));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action.Id, Is.EqualTo("/workflows/test/runs/run123/actions/CompleteAction"));
            Assert.That(action.Status, Is.EqualTo("Succeeded"));
            Assert.That(action.Code, Is.EqualTo("OK"));
            Assert.That(action.CanResubmit, Is.True);
            Assert.That(action.DesignerName, Is.EqualTo("CompleteAction"));
            Assert.That(action.StartTime, Is.Not.Null);
            Assert.That(action.EndTime, Is.Not.Null);
            Assert.That(action.ScheduledTime, Is.Not.Null);
            Assert.That(action.OriginHistoryName, Is.EqualTo("original-run-456"));
            Assert.That(action.TrackingId, Is.EqualTo("tracking-abc-123"));
            Assert.That(action.IterationCount, Is.EqualTo(3));
            Assert.That(action.RepetitionCount, Is.EqualTo(5));
            Assert.That(action.Correlation, Is.Not.Null);
            Assert.That(action.Correlation!.ClientTrackingId, Is.EqualTo("client-xyz"));
        }
    }

    [Test]
    public async Task LoadActionDetails_Should_Replace_Underscores_In_DesignerName()
    {
        // Arrange
        var action = new MockAction("Initialize_my_variable_name", ActionType.Action);

        const string actionDetailsJson = 
            """
             {
                 "id": "/workflows/test/runs/run123/actions/Initialize_my_variable_name",
                 "name": "Initialize_my_variable_name",
                 "type": "workflows/runs/actions",
                 "properties": {
                     "status": "Succeeded"
                 }
             }
             """;

        var actionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(actionDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetailsAction>(Arg.Any<Uri>())
            .Returns(Task.FromResult(actionDetails));

        // Act
        await action.LoadActionDetails(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        Assert.That(action.DesignerName, Is.EqualTo("Initialize my variable name"));
    }

    private Task<WorkflowRunDetailsAction?> ReturnActionDetails(CallInfo arg)
    {
        return Task.FromResult(_actionDetailsResponse);
    }

    private Task<JToken?> ReturnActionInputContent(CallInfo arg)
    {
        return Task.FromResult(_actionInputContent);
    }

    private Task<Response<WorkflowRunDetailsAction>?> ReturnActionRepetitions(CallInfo arg)
    {
        return Task.FromResult(_actionRepetitionsResponse);
    }
}