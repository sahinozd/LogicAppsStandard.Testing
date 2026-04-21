using LogicApps.Management.Actions;
using LogicApps.Management.Helper;
using LogicApps.Management.Models.Enums;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;

namespace LogicApps.Management.Tests.Actions;

[TestFixture]
internal sealed class ForEachActionTests
{
    private IConfiguration _configuration;
    private IAzureManagementRepository _azureManagementRepository;
    private IActionHelper _actionHelper;

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
    }

    [TearDown]
    public void TearDown()
    {
        _configuration = null!;
        _azureManagementRepository = null!;
        _actionHelper = null!;
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_When_Valid_Parameters_Are_Provided()
    {
        // Arrange
        const string actionName = "Foreach_item";
        const ActionType actionType = ActionType.ForEach;

        // Act
        var forEachAction = new ForEachAction(actionName, actionType);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(forEachAction, Is.Not.Null);
            Assert.That(forEachAction.Name, Is.EqualTo(actionName));
            Assert.That(forEachAction.Type, Is.EqualTo(actionType));
            Assert.That(forEachAction.Repetitions, Is.Not.Null);
            Assert.That(forEachAction.Repetitions, Is.Empty);
        }
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_Name_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new ForEachAction(null!, ActionType.ForEach);
        });

        // Assert
        Assert.That(argumentNullException?.ParamName, Is.EqualTo("name"));
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentException_When_Name_Is_Empty()
    {
        // Arrange & Act
        var argumentException = Assert.Throws<ArgumentException>(() =>
        {
            _ = new ForEachAction(string.Empty, ActionType.ForEach);
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
            _ = new ForEachAction("Foreach_item", (ActionType)999);
        });

        // Assert
        Assert.That(argumentOutOfRangeException?.ParamName, Is.EqualTo("actionType"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_Configuration_Is_Null()
    {
        // Arrange
        var forEachAction = new ForEachAction("Foreach_item", ActionType.ForEach);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => forEachAction.GetAllActionRepetitions(null!, _azureManagementRepository, _actionHelper, "workflow", "123"));
        Assert.That(argumentNullException.ParamName, Is.EqualTo("configuration"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_AzureManagementRepository_Is_Null()
    {
        // Arrange
        var forEachAction = new ForEachAction("Foreach_item", ActionType.ForEach);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => 
            forEachAction.GetAllActionRepetitions(_configuration, null!, _actionHelper, "workflow", "123"));
        Assert.That(argumentNullException.ParamName, Is.EqualTo("azureManagementRepository"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_ActionHelper_Is_Null()
    {
        // Arrange
        var forEachAction = new ForEachAction("Foreach_item", ActionType.ForEach);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => 
            forEachAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, null!, "workflow", "123"));
        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionHelper"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_WorkflowName_Is_Null()
    {
        // Arrange
        var forEachAction = new ForEachAction("Foreach_item", ActionType.ForEach);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => 
            forEachAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, null!, "run"));
        Assert.That(argumentNullException.ParamName, Is.EqualTo("workflowName"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentException_When_WorkflowName_Is_Empty()
    {
        // Arrange
        var forEachAction = new ForEachAction("Foreach_item", ActionType.ForEach);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentException>(() => 
            forEachAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, string.Empty, "run"));
        Assert.That(argumentException.ParamName, Is.EqualTo("workflowName"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_RunId_Is_Null()
    {
        // Arrange
        var forEachAction = new ForEachAction("Foreach_item", ActionType.ForEach);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => 
            forEachAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", null!));
        Assert.That(argumentNullException.ParamName, Is.EqualTo("runId"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentException_When_RunId_Is_Empty()
    {
        // Arrange
        var forEachAction = new ForEachAction("Foreach_item", ActionType.ForEach);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentException>(() => 
            forEachAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", string.Empty));
        Assert.That(argumentException.ParamName, Is.EqualTo("runId"));
    }

    [Test]
    public async Task GetAllActionRepetitions_Should_Return_Empty_List_When_No_Repetitions_Exist()
    {
        // Arrange
        var forEachAction = new ForEachAction("Foreach_item", ActionType.ForEach);

        var emptyResponse = new Response<WorkflowRunDetailsActionRepetition>
        {
            Value = new List<WorkflowRunDetailsActionRepetition>(),
            NextLink = null
        };

        // Configure the mock to return an empty response
        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>())
            .Returns(emptyResponse);

        // Act
        var result = await forEachAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", "123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        // Verify that GetObject was called exactly once
        await _azureManagementRepository.Received(1).GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetAllActionRepetitions_Should_Return_Repetitions_When_Single_Page_Exists()
    {
        // Arrange
        var forEachAction = new ForEachAction("Foreach_item", ActionType.ForEach);

        var repetition1 = new WorkflowRunDetailsActionRepetition
        {
            Id = "/workflows/workflow-name/runs/run-id/actions/Foreach_item/scopeRepetitions/000000",
            Name = "000000",
            Type = "workflows/run/actions/scoperepetitions",
            Properties = new WorkflowRunDetailsActionRepetitionProperties
            {
                Code = "OK",
                Status = "Succeeded",
                RepetitionIndexes = [new() { ScopeName = "Foreach_item", ItemIndex = 0 }]
            }
        };

        var repetition2 = new WorkflowRunDetailsActionRepetition
        {
            Id = "/workflows/workflow-name/runs/run-id/actions/Foreach_item/scopeRepetitions/000001",
            Name = "000001",
            Type = "workflows/run/actions/scoperepetitions",
            Properties = new WorkflowRunDetailsActionRepetitionProperties
            {
                Code = "OK",
                Status = "Succeeded",
                RepetitionIndexes = [new() { ScopeName = "Foreach_item", ItemIndex = 1 }]
            }
        };

        var response = new Response<WorkflowRunDetailsActionRepetition>
        {
            Value = new List<WorkflowRunDetailsActionRepetition> { repetition1, repetition2 },
            NextLink = null
        };

        // Configure the mock to return the response
        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>())
            .Returns(response);

        // Act
        var result = await forEachAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow1", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("000000"));
            Assert.That(result[1].Name, Is.EqualTo("000001"));
        }

        // Verify that GetObject was called exactly once
        await _azureManagementRepository.Received(1).GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetAllActionRepetitions_Should_Follow_NextLink_When_Multiple_Pages_Exist()
    {
        // Arrange
        var forEachAction = new ForEachAction("Foreach_item", ActionType.ForEach);

        var repetition1 = new WorkflowRunDetailsActionRepetition
        {
            Id = "/workflows/workflow-name/runs/run-id/actions/Foreach_item/scopeRepetitions/000000",
            Name = "000000",
            Type = "workflows/run/actions/scoperepetitions",
            Properties = new WorkflowRunDetailsActionRepetitionProperties
            {
                Code = "OK",
                Status = "Succeeded",
                RepetitionIndexes = [new() { ScopeName = "Foreach_item", ItemIndex = 0 }]
            }
        };

        var repetition2 = new WorkflowRunDetailsActionRepetition
        {
            Id = "/workflows/workflow-name/runs/run-id/actions/Foreach_item/scopeRepetitions/000001",
            Name = "000001",
            Type = "workflows/run/actions/scoperepetitions",
            Properties = new WorkflowRunDetailsActionRepetitionProperties
            {
                Code = "OK",
                Status = "Succeeded",
                RepetitionIndexes = [new() { ScopeName = "Foreach_item", ItemIndex = 1 }]
            }
        };

        var firstPageResponse = new Response<WorkflowRunDetailsActionRepetition>
        {
            Value = new List<WorkflowRunDetailsActionRepetition> { repetition1 },
            NextLink = "https://management.azure.com/subscriptions/subscription-id/resourceGroups/resource-group/providers/Microsoft.Web/sites/logicapp-name/hostruntime/runtime/webhooks/workflow/api/management/workflows/workflow-name/runs/wun-id/actions/For_each_action_3/scopeRepetitions?api_version=2025-05-01&skipToken=token&code=key"
        };

        var secondPageResponse = new Response<WorkflowRunDetailsActionRepetition>
        {
            Value = new List<WorkflowRunDetailsActionRepetition> { repetition2 },
            NextLink = null
        };

        // Configure the mock to return different responses based on call count
        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>())
            .Returns(firstPageResponse, secondPageResponse);

        // Act
        var result = await forEachAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow1", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("000000"));
            Assert.That(result[1].Name, Is.EqualTo("000001"));
        }

        // Verify that GetObject was called exactly twice (once for each page)
        await _azureManagementRepository.Received(2).GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }
}