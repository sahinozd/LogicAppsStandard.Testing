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
internal sealed class UntilActionTests
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
        const string actionName = "Until";
        const ActionType actionType = ActionType.Until;

        // Act
        var untilAction = new UntilAction(actionName, actionType);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(untilAction, Is.Not.Null);
            Assert.That(untilAction.Name, Is.EqualTo(actionName));
            Assert.That(untilAction.Type, Is.EqualTo(actionType));
            Assert.That(untilAction.Repetitions, Is.Not.Null);
            Assert.That(untilAction.Repetitions, Is.Empty);
        }
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_Name_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new UntilAction(null!, ActionType.Until);
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
            _ = new UntilAction(string.Empty, ActionType.Until);
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
            _ = new UntilAction("Until", (ActionType)999);
        });

        // Assert
        Assert.That(argumentOutOfRangeException?.ParamName, Is.EqualTo("actionType"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_Configuration_Is_Null()
    {
        // Arrange
        var untilAction = new UntilAction("Until", ActionType.Until);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => 
            untilAction.GetAllActionRepetitions(null!, _azureManagementRepository, _actionHelper, "workflow", "123", null));
        Assert.That(argumentNullException.ParamName, Is.EqualTo("configuration"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_AzureManagementRepository_Is_Null()
    {
        // Arrange
        var untilAction = new UntilAction("Until", ActionType.Until);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => 
            untilAction.GetAllActionRepetitions(_configuration, null!, _actionHelper, "workflow", "123", null));
        Assert.That(argumentNullException.ParamName, Is.EqualTo("azureManagementRepository"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_ActionHelper_Is_Null()
    {
        // Arrange
        var untilAction = new UntilAction("Until", ActionType.Until);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => 
            untilAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, null!, "workflow", "123", null));
        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionHelper"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_WorkflowName_Is_Null()
    {
        // Arrange
        var untilAction = new UntilAction("Until", ActionType.Until);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => 
            untilAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, null!, "123", null));
        Assert.That(argumentNullException.ParamName, Is.EqualTo("workflowName"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentException_When_WorkflowName_Is_Empty()
    {
        // Arrange
        var untilAction = new UntilAction("Until", ActionType.Until);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentException>(() => 
            untilAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, string.Empty, "123", null));
        Assert.That(argumentException.ParamName, Is.EqualTo("workflowName"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentNullException_When_RunId_Is_Null()
    {
        // Arrange
        var untilAction = new UntilAction("Until", ActionType.Until);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => 
            untilAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", null!, null));
        Assert.That(argumentNullException.ParamName, Is.EqualTo("runId"));
    }

    [Test]
    public void GetAllActionRepetitions_Should_Throw_ArgumentException_When_RunId_Is_Empty()
    {
        // Arrange
        var untilAction = new UntilAction("Until", ActionType.Until);

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentException>(() => 
            untilAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", string.Empty, null));
        Assert.That(argumentException.ParamName, Is.EqualTo("runId"));
    }

    [Test]
    public async Task GetAllActionRepetitions_Should_Return_Empty_List_When_IterationCount_Is_Zero()
    {
        // Arrange
        var untilAction = new UntilAction("Until", ActionType.Until)
        {
            IterationCount = 0
        };

        // Act
        var result = await untilAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", "123", null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        // Verify that repository was not called
        await _azureManagementRepository.DidNotReceive().GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetAllActionRepetitions_Should_Return_Repetitions_Based_On_IterationCount_When_RepetitionIndex_Is_Null()
    {
        // Arrange
        var untilAction = new UntilAction("Until", ActionType.Until)
        {
            IterationCount = 3,
            TrackingId = "tracking-id",
            Code = "OK",
            CanResubmit = true,
            Correlation = new Correlation
            {
                ActionTrackingId = "action-tracking",
                ClientTrackingId = "client-tracking"
            }
        };

        // Act
        var result = await untilAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", "123", null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result[0].Name, Is.EqualTo("000000"));
            Assert.That(result[1].Name, Is.EqualTo("000001"));
            Assert.That(result[2].Name, Is.EqualTo("000002"));
            Assert.That(result[0].Id, Does.Contain("repetitions/000000"));
            Assert.That(result[1].Id, Does.Contain("repetitions/000001"));
            Assert.That(result[2].Id, Does.Contain("repetitions/000002"));
        }

        // Verify that repository was not called when repetitionIndex is null
        await _azureManagementRepository.DidNotReceive().GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetAllActionRepetitions_Should_Query_API_When_RepetitionIndex_Is_Provided()
    {
        // Arrange
        var untilAction = new UntilAction("Until", ActionType.Until)
        {
            IterationCount = 2,
            TrackingId = "tracking-id"
        };

        var apiRepetition = new WorkflowRunDetailsActionRepetition
        {
            Id = "/workflows/workflow/runs/123/actions/Until/repetitions/000001",
            Name = "000001",
            Type = "workflows/run/actions/repetitions",
            Properties = new WorkflowRunDetailsActionRepetitionProperties
            {
                Code = "OK",
                Status = "Succeeded",
                TrackingId = "tracking-id",
                IterationCount = 2,
                CanResubmit = true,
                StartTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 1, 15, 10, 0, 10, DateTimeKind.Utc),
                Correlation = new Correlation
                {
                    ActionTrackingId = "action-tracking",
                    ClientTrackingId = "client-tracking"
                },
                RepetitionIndexes = [new RepetitionIndex { ScopeName = "ParentScope", ItemIndex = 1 }]
            }
        };

        var response = new Response<WorkflowRunDetailsActionRepetition>
        {
            Value = new List<WorkflowRunDetailsActionRepetition> { apiRepetition },
            NextLink = null
        };

        // Configure the mock to return the response
        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>())
            .Returns(response);

        // Act
        var result = await untilAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", "123", "000001").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("000001-000000"));
            Assert.That(result[1].Name, Is.EqualTo("000001-000001"));
            Assert.That(result[0].TrackingId, Is.EqualTo("tracking-id"));
            Assert.That(result[1].TrackingId, Is.EqualTo("tracking-id"));
        }

        // Verify that GetObject was called exactly once
        await _azureManagementRepository.Received(1).GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetAllActionRepetitions_Should_Return_Empty_List_When_RepetitionIndex_Not_Found_In_API()
    {
        // Arrange
        var untilAction = new UntilAction("Until", ActionType.Until)
        {
            IterationCount = 2
        };

        var apiRepetitionDifferentName = new WorkflowRunDetailsActionRepetition
        {
            Id = "/workflows/workflow/runs/123/actions/Until/repetitions/000002",
            Name = "000002",
            Type = "workflows/run/actions/repetitions",
            Properties = new WorkflowRunDetailsActionRepetitionProperties
            {
                Code = "OK",
                Status = "Succeeded",
                RepetitionIndexes = []
            }
        };

        var response = new Response<WorkflowRunDetailsActionRepetition>
        {
            Value = new List<WorkflowRunDetailsActionRepetition> { apiRepetitionDifferentName },
            NextLink = null
        };

        // Configure the mock to return the response
        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>())
            .Returns(response);

        // Act
        var result = await untilAction.GetAllActionRepetitions(_configuration, _azureManagementRepository, _actionHelper, "workflow", "123", "000001").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        // Verify that GetObject was called exactly once
        await _azureManagementRepository.Received(1).GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }
}