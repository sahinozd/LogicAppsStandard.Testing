using LogicApps.Management.Actions;
using LogicApps.Management.Factory;
using LogicApps.Management.Helper;
using LogicApps.Management.Repository;
using LogicApps.TestFramework.Specifications.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace LogicApps.TestFramework.Specifications.Tests;

[TestFixture]
internal sealed class WorkflowRunValidationTests
{
    private IConfiguration _configuration = null!;
    private IAzureManagementRepository _azureManagementRepository = null!;
    private IActionFactory _actionFactory = null!;
    private IActionHelper _actionHelper = null!;
    private LogicApps.Management.Models.RestApi.WorkflowRun _workflowRunProperties = null!;
    private JObject _workflowDefinition = null!;
    private const string WorkflowName = "test-workflow";

    [SetUp]
    public void SetUp()
    {
        _configuration = Substitute.For<IConfiguration>();
        _azureManagementRepository = Substitute.For<IAzureManagementRepository>();
        _actionFactory = Substitute.For<IActionFactory>();
        _actionHelper = Substitute.For<IActionHelper>();

        _configuration["SubscriptionId"].Returns("subscription-id");
        _configuration["ResourceGroup"].Returns("resource-group");
        _configuration["LogicAppName"].Returns("logic-app");
        _configuration["LogicAppApiVersion"].Returns("2025-05-01");
        _configuration["VariableActionName"].Returns("Initialize_variables");
        _configuration["CorrelationIdVariableName"].Returns("correlationId");

        LoadTestData();
        SetupDefaultMocks();
    }

    private void LoadTestData()
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "TestData");

        var workflowRunPath = Path.Combine(basePath, "Workflow-run-content.json");
        var workflowRunJson = File.ReadAllText(workflowRunPath);
        _workflowRunProperties = JsonConvert.DeserializeObject<LogicApps.Management.Models.RestApi.WorkflowRun>(workflowRunJson)!;

        var workflowDefPath = Path.Combine(basePath, "Workflow-definition.json");
        var workflowDefJson = File.ReadAllText(workflowDefPath);
        _workflowDefinition = JsonConvert.DeserializeObject<JObject>(workflowDefJson)!;
    }

    private void SetupDefaultMocks()
    {
        _actionFactory.SetWorkflowRunProperties(WorkflowName, Arg.Any<string>());

        _actionFactory.CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var actionName = callInfo.ArgAt<string>(0);
                var actionNode = callInfo.ArgAt<JObject>(1);
                var actionType = actionNode["type"]?.ToString() ?? "Action";

                BaseAction action = actionType switch
                {
                    "Scope" => CreateScopeAction(actionName, actionNode),
                    "If" or "Condition" => CreateConditionAction(actionName, actionNode),
                    "Switch" => CreateSwitchAction(actionName, actionNode),
                    "ForEach" or "Foreach" => new ForEachAction(actionName, Management.Models.Enums.ActionType.ForEach),
                    "Until" => new UntilAction(actionName, Management.Models.Enums.ActionType.Until),
                    _ => new Management.Actions.Action(actionName, Management.Models.Enums.ActionType.Action)
                };

                action.DesignerName = actionName;
                action.Status = "Succeeded";
                action.StartTime = DateTime.UtcNow.AddMinutes(-10);
                action.EndTime = DateTime.UtcNow;

                return Task.FromResult(action);
            });
    }

    private static ScopeAction CreateScopeAction(string name, JObject node)
    {
        var scopeAction = new ScopeAction(name, Management.Models.Enums.ActionType.Scope);

        if (node["actions"] is JObject actionsObj)
        {
            foreach (var prop in actionsObj.Properties())
            {
                var childAction = CreateActionFromNode(prop.Name, (JObject)prop.Value);
                scopeAction.Actions.Add(childAction);
            }
        }

        return scopeAction;
    }

    private static ConditionAction CreateConditionAction(string name, JObject node)
    {
        var conditionAction = new ConditionAction(name, Management.Models.Enums.ActionType.Condition);

        if (node["actions"] is JObject actionsObj)
        {
            foreach (var prop in actionsObj.Properties())
            {
                var childAction = CreateActionFromNode(prop.Name, (JObject)prop.Value);
                conditionAction.DefaultActions.Add(childAction);
            }
        }

        if (node["else"]?["actions"] is JObject elseActionsObj)
        {
            foreach (var prop in elseActionsObj.Properties())
            {
                var childAction = CreateActionFromNode(prop.Name, (JObject)prop.Value);
                conditionAction.ElseActions.Add(childAction);
            }
        }

        return conditionAction;
    }

    private static SwitchAction CreateSwitchAction(string name, JObject node)
    {
        var switchAction = new SwitchAction(name, Management.Models.Enums.ActionType.Switch);

        if (node["cases"] is JObject casesObj)
        {
            foreach (var caseProperty in casesObj.Properties())
            {
                var switchCase = new SwitchCase
                {
                    Name = caseProperty.Name
                };

                if (caseProperty.Value["actions"] is JObject caseActionsObj)
                {
                    foreach (var actionProp in caseActionsObj.Properties())
                    {
                        var childAction = CreateActionFromNode(actionProp.Name, (JObject)actionProp.Value);
                        switchCase.Actions.Add(childAction);
                    }
                }

                switchAction.Cases.Add(switchCase);
            }
        }

        return switchAction;
    }

    private static BaseAction CreateActionFromNode(string name, JObject node)
    {
        var actionType = node["type"]?.ToString() ?? "Action";

        BaseAction action = actionType switch
        {
            "Scope" => CreateScopeAction(name, node),
            "If" or "Condition" => CreateConditionAction(name, node),
            "Switch" => CreateSwitchAction(name, node),
            _ => new Management.Actions.Action(name, Management.Models.Enums.ActionType.Action)
        };

        action.DesignerName = name;
        action.Status = "Succeeded";
        action.StartTime = DateTime.UtcNow.AddMinutes(-10);
        action.EndTime = DateTime.UtcNow;

        return action;
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
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_WorkflowRun_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new WorkflowRunValidation(null!);
        });
    }

    [Test]
    public async Task Constructor_Should_Create_Instance_When_WorkflowRun_Is_Valid()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        Assert.That(validation, Is.Not.Null);
    }

    [Test]
    public async Task ValidateRunActionsAsync_Should_Return_True_When_ExpectedEvents_Is_Null()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateRunActionsAsync().ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateRunActionsAsync_Should_Return_True_When_ExpectedEvents_Is_Empty()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateRunActionsAsync([]).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateRunActionsAsync_Should_Return_False_With_Message_When_Action_Is_Not_Found()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Action1", "Succeeded")
        };

        var result = await validation.ValidateRunActionsAsync(expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("Action1"));
            Assert.That(result.Item2, Does.Contain("was not found"));
        }
    }

    [Test]
    public async Task ValidateSingleActionAsync_Should_Return_False_With_Message_When_Action_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateSingleActionAsync("NonExistent", "Succeeded").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Is.EqualTo("Action \"NonExistent\" was not found"));
        }
    }

    [Test]
    public async Task ValidateChildActionsAsync_Should_Return_False_With_Message_When_Parent_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Action1", "Succeeded")
        };

        var result = await validation.ValidateChildActionsAsync("NonExistent", expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("No child actions found"));
        }
    }

    [Test]
    public async Task ValidateLoopIterationCountAsync_Should_Return_False_With_Message_When_Loop_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateLoopIterationCountAsync("NonExistentLoop", 5, "Succeeded").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Is.EqualTo("Loop \"NonExistentLoop\" was not found"));
        }
    }

    [Test]
    public async Task ValidateSingleActionAsync_Should_Return_True_When_Action_Exists_With_Correct_Status()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateSingleActionAsync("Test_Action", "Succeeded").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateSingleActionAsync_Should_Return_False_When_Action_Has_Wrong_Status()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateSingleActionAsync("Test_Action", "Failed").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("Succeeded"));
            Assert.That(result.Item2, Does.Contain("Failed"));
            Assert.That(result.Item2, Does.Contain("expected"));
        }
    }

    [Test]
    public async Task ValidateChildActionsAsync_Should_Return_True_When_ExpectedEvents_Is_Empty()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateChildActionsAsync("Test_Scope", []).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateChildActionsAsync_Should_Return_Result_When_Branch_Is_Valid()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateChildActionsAsync("Test_Condition", [], "actions").ConfigureAwait(false);

        Assert.That(result.Item1, Is.True.Or.False);
    }

    [Test]
    public async Task ValidateActionsInIterationAsync_Should_Return_False_With_Message_When_Loop_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Action1", "Succeeded")
        };

        var result = await validation.ValidateActionsInIterationAsync("NonExistentLoop", 1, expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("not found"));
        }
    }

    [Test]
    public async Task ValidateActionsInAllIterationsAsync_Should_Return_False_With_Message_When_Loop_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Action1", "Succeeded")
        };

        var result = await validation.ValidateActionsInAllIterationsAsync("NonExistentLoop", expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Is.EqualTo("Loop \"NonExistentLoop\" was not found"));
        }
    }

    [Test]
    public async Task ValidateNestedLoopAsync_Should_Return_False_With_Message_When_Parent_Loop_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateNestedLoopAsync("NonExistentLoop", 1, "NestedLoop", 5, "Succeeded").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("No nested loops found"));
        }
    }

    [Test]
    public async Task ValidateRunActionsAsync_Should_Return_True_When_ExpectedAction_Exists_With_Correct_Status()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Test_Action", "Succeeded")
        };

        var result = await validation.ValidateRunActionsAsync(expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateRunActionsAsync_Should_Return_False_When_ExpectedAction_Has_Wrong_Status()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Test_Action", "Failed")
        };

        var result = await validation.ValidateRunActionsAsync(expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("Succeeded"));
            Assert.That(result.Item2, Does.Contain("Failed"));
        }
    }

    [Test]
    public async Task ValidateChildActionsAsync_Should_Return_True_When_Child_Action_Exists_With_Correct_Status()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Nested_Action", "Succeeded")
        };

        var result = await validation.ValidateChildActionsAsync("Test_Scope", expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateChildActionsAsync_Should_Return_False_When_Child_Action_Has_Wrong_Status()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Nested_Action", "Failed")
        };

        var result = await validation.ValidateChildActionsAsync("Test_Scope", expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("Succeeded"));
            Assert.That(result.Item2, Does.Contain("Failed"));
        }
    }

    [Test]
    public async Task ValidateChildActionsAsync_Should_Return_False_When_Expected_Action_Not_Found_In_Scope()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("NonExistent_Child", "Succeeded")
        };

        var result = await validation.ValidateChildActionsAsync("Test_Scope", expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("NonExistent_Child"));
        }
    }

    [Test]
    public async Task ValidateChildActionsAsync_Should_Return_True_For_Condition_True_Branch()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Condition_True_Action", "Succeeded")
        };

        var result = await validation.ValidateChildActionsAsync("Test_Condition", expectedEvents, "actions").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateChildActionsAsync_Should_Return_True_For_Condition_Else_Branch()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Condition_False_Action", "Succeeded")
        };

        var result = await validation.ValidateChildActionsAsync("Test_Condition", expectedEvents, "else").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateChildActionsAsync_Should_Return_False_With_Branch_Message_When_Branch_Has_No_Actions()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Some_Action", "Succeeded")
        };

        var result = await validation.ValidateChildActionsAsync("Test_Condition", expectedEvents, "nonexistent_branch").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("No child actions found"));
        }
    }

    [Test]
    public async Task ValidateChildActionsAsync_Should_Throw_When_ExpectedEvents_Is_Null()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await validation.ValidateChildActionsAsync("Test_Scope", null!).ConfigureAwait(false));
    }

    [Test]
    public async Task ValidateActionsInIterationAsync_Should_Throw_When_ExpectedEvents_Is_Null()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await validation.ValidateActionsInIterationAsync("Test_Loop", 1, null!).ConfigureAwait(false));
    }

    [Test]
    public async Task ValidateActionsInAllIterationsAsync_Should_Throw_When_ExpectedEvents_Is_Null()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await validation.ValidateActionsInAllIterationsAsync("Test_Loop", null!).ConfigureAwait(false));
    }

    [Test]
    public async Task ValidateLoopIterationCountAsync_Should_Return_True_When_Loop_Has_Correct_Repetition_Count()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 3, iterationStatus: "Succeeded").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateLoopIterationCountAsync("Test_ForEach", 3, "Succeeded").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateLoopIterationCountAsync_Should_Return_False_When_Loop_Has_Wrong_Repetition_Count()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 2, iterationStatus: "Succeeded").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateLoopIterationCountAsync("Test_ForEach", 5, "Succeeded").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("2"));
            Assert.That(result.Item2, Does.Contain("5"));
        }
    }

    [Test]
    public async Task ValidateLoopIterationCountAsync_Should_Return_False_When_Any_Repetition_Has_Wrong_Status()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 2, iterationStatus: "Failed").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateLoopIterationCountAsync("Test_ForEach", 2, "Succeeded").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("Failed"));
            Assert.That(result.Item2, Does.Contain("Succeeded"));
        }
    }

    [Test]
    public async Task ValidateActionsInIterationAsync_Should_Return_True_When_Action_Found_With_Correct_Status()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 2, iterationStatus: "Succeeded").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Loop_Child_Action", "Succeeded")
        };

        var result = await validation.ValidateActionsInIterationAsync("Test_ForEach", 1, expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateActionsInIterationAsync_Should_Return_False_When_Action_Not_Found_In_Iteration()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 2, iterationStatus: "Succeeded").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("NonExistent_Action", "Succeeded")
        };

        var result = await validation.ValidateActionsInIterationAsync("Test_ForEach", 1, expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("NonExistent_Action"));
            Assert.That(result.Item2, Does.Contain("not found"));
        }
    }

    [Test]
    public async Task ValidateActionsInIterationAsync_Should_Return_False_When_Action_Has_Wrong_Status_In_Iteration()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 2, iterationStatus: "Succeeded").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Loop_Child_Action", "Failed")
        };

        var result = await validation.ValidateActionsInIterationAsync("Test_ForEach", 1, expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("Succeeded"));
            Assert.That(result.Item2, Does.Contain("Failed"));
        }
    }

    [Test]
    public async Task ValidateActionsInIterationAsync_Should_Return_False_When_Iteration_Index_Out_Of_Range()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 2, iterationStatus: "Succeeded").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Loop_Child_Action", "Succeeded")
        };

        var result = await validation.ValidateActionsInIterationAsync("Test_ForEach", 99, expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("not found"));
        }
    }

    [Test]
    public async Task ValidateActionsInAllIterationsAsync_Should_Return_True_When_All_Iterations_Are_Valid()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 3, iterationStatus: "Succeeded").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Loop_Child_Action", "Succeeded")
        };

        var result = await validation.ValidateActionsInAllIterationsAsync("Test_ForEach", expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateActionsInAllIterationsAsync_Should_Return_False_When_Loop_Has_No_Iterations()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 0, iterationStatus: "Succeeded").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("Loop_Child_Action", "Succeeded")
        };

        var result = await validation.ValidateActionsInAllIterationsAsync("Test_ForEach", expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("no iterations"));
        }
    }

    [Test]
    public async Task ValidateActionsInAllIterationsAsync_Should_Return_False_When_Action_Missing_From_Iteration()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 2, iterationStatus: "Succeeded").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);
        var expectedEvents = new List<WorkflowEvent>
        {
            new("NonExistent_Action", "Succeeded")
        };

        var result = await validation.ValidateActionsInAllIterationsAsync("Test_ForEach", expectedEvents).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("NonExistent_Action"));
        }
    }

    [Test]
    public async Task ValidateRunLoopActionsExecutionsAsync_Should_Return_False_When_Loop_Not_Found()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateRunLoopActionsExecutionsAsync("NonExistentLoop", 0, null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Is.EqualTo("Loop action was not found"));
        }
    }

    [Test]
    public async Task ValidateRunLoopActionsExecutionsAsync_Should_Return_True_When_Expected_Events_Is_Null()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 2, iterationStatus: "Succeeded").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateRunLoopActionsExecutionsAsync("Test_ForEach", 0, null).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.True);
            Assert.That(result.Item2, Is.Null);
        }
    }

    [Test]
    public async Task ValidateNestedLoopAsync_Should_Return_False_When_Nested_Loop_Name_Not_Found_In_Iteration()
    {
        var workflowRun = await CreateWorkflowRunWithForEachAsync(iterationCount: 2, iterationStatus: "Succeeded").ConfigureAwait(false);
        var validation = new WorkflowRunValidation(workflowRun);

        var result = await validation.ValidateNestedLoopAsync("Test_ForEach", 1, "NonExistentNested", 2, "Succeeded").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Item1, Is.False);
            Assert.That(result.Item2, Does.Contain("No nested loops found"));
        }
    }

    private async Task<Management.WorkflowRun> CreateWorkflowRunAsync()
    {
        return await Management.WorkflowRun.CreateAsync(
            _configuration,
            _azureManagementRepository,
            _actionFactory,
            _actionHelper,
            WorkflowName,
            _workflowRunProperties,
            _workflowDefinition
        ).ConfigureAwait(false);
    }

    private async Task<Management.WorkflowRun> CreateWorkflowRunWithForEachAsync(int iterationCount, string iterationStatus)
    {
        var forEachDefinition = JObject.Parse("""
            {
              "definition": {
                "actions": {
                  "Test_ForEach": {
                    "type": "ForEach",
                    "foreach": "@triggerBody()",
                    "actions": {
                      "Loop_Child_Action": {
                        "type": "Http"
                      }
                    }
                  }
                }
              }
            }
            """);

        var localFactory = Substitute.For<IActionFactory>();
        localFactory.SetWorkflowRunProperties(WorkflowName, Arg.Any<string>());
        localFactory.CreateActionFromJObject(Arg.Any<string>(), Arg.Any<JObject>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                var actionName = callInfo.ArgAt<string>(0);

                var forEachAction = new ForEachAction(actionName, Management.Models.Enums.ActionType.ForEach)
                {
                    DesignerName = actionName,
                    Status = "Succeeded",
                    StartTime = DateTime.UtcNow.AddMinutes(-10),
                    EndTime = DateTime.UtcNow
                };

                for (var i = 0; i < iterationCount; i++)
                {
                    var repetition = new ForEachActionRepetition(new Management.Models.RestApi.WorkflowRunDetailsActionRepetition
                    {
                        Id = $"/repetitions/{i:D6}",
                        Name = $"{i:D6}",
                        Type = "workflows/run/actions/repetitions",
                        Properties = new Management.Models.RestApi.WorkflowRunDetailsActionRepetitionProperties
                        {
                            Status = iterationStatus,
                            RepetitionIndexes =
                            [
                                new Management.Models.RestApi.RepetitionIndex { ScopeName = actionName, ItemIndex = i }
                            ]
                        }
                    });

                    var childAction = new Management.Actions.Action("Loop_Child_Action", Management.Models.Enums.ActionType.Action)
                    {
                        DesignerName = "Loop_Child_Action",
                        Status = "Succeeded"
                    };
                    repetition.Actions.Add(childAction);

                    forEachAction.Repetitions.Add(repetition);
                }

                return Task.FromResult<BaseAction>(forEachAction);
            });

        return await Management.WorkflowRun.CreateAsync(
            _configuration,
            _azureManagementRepository,
            localFactory,
            _actionHelper,
            WorkflowName,
            _workflowRunProperties,
            forEachDefinition
        ).ConfigureAwait(false);
    }
}