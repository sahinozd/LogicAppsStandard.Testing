using LogicApps.Management.Actions;
using LogicApps.Management.Factory;
using LogicApps.Management.Helper;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace LogicApps.TestFramework.Specifications.Tests;

[TestFixture]
internal sealed class WorkflowRunNavigatorTests
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
            _ = new WorkflowRunNavigator(null!);
        });
    }

    [Test]
    public async Task Constructor_Should_Create_Instance_When_WorkflowRun_Is_Valid()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        Assert.That(navigator, Is.Not.Null);
    }

    [Test]
    public async Task GetTopLevelActionsAsync_Should_Return_Top_Level_Actions_When_Test_Data_Is_Loaded()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetTopLevelActionsAsync().ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(5));
        Assert.That(result.Select(a => a.Name), Does.Contain("Initialize_variables"));
        Assert.That(result.Select(a => a.Name), Does.Contain("Test_Action"));
        Assert.That(result.Select(a => a.Name), Does.Contain("Test_Scope"));
        Assert.That(result.Select(a => a.Name), Does.Contain("Test_Condition"));
        Assert.That(result.Select(a => a.Name), Does.Contain("Test_Switch"));
    }

    [Test]
    public async Task GetChildActionsAsync_Should_Return_Empty_When_Parent_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetChildActionsAsync("NonExistent").ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task FindActionAsync_Should_Return_Empty_When_Action_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.FindActionAsync("NonExistent").ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetActionsInScopeAsync_Should_Return_Empty_When_Scope_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetActionsInScopeAsync("NonExistent", "branch").ConfigureAwait(false);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task FindActionAsync_Should_Return_Action_When_Action_Exists()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.FindActionAsync("Test_Action").ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result![0].Name, Is.EqualTo("Test_Action"));
    }

    [Test]
    public async Task FindActionAsync_Should_Return_Null_When_ActionName_Is_Null()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.FindActionAsync(null!).ConfigureAwait(false);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task FindActionAsync_Should_Return_Null_When_ActionName_Is_Empty()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.FindActionAsync(string.Empty).ConfigureAwait(false);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetChildActionsAsync_Should_Return_Child_Actions_When_Action_Is_Scope()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetChildActionsAsync("Test_Scope").ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task GetActionsInScopeAsync_Should_Return_Actions_When_Scope_Is_Valid()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetActionsInScopeAsync("Test_Scope").ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task GetActionsInScopeAsync_Should_Return_Actions_When_Condition_Has_True_Branch()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetActionsInScopeAsync("Test_Condition", "actions").ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetActionsInScopeAsync_Should_Return_Actions_When_Condition_Has_False_Branch()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetActionsInScopeAsync("Test_Condition", "else").ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetActionsInScopeAsync_Should_Return_Actions_When_Switch_Has_Case_Name()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetActionsInScopeAsync("Test_Switch", "Case_1").ConfigureAwait(false);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetActionsInForEachIterationAsync_Should_Return_Null_When_Loop_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetActionsInForEachIterationAsync("NonExistentLoop", 0).ConfigureAwait(false);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetActionsInUntilIterationAsync_Should_Return_Null_When_Loop_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetActionsInUntilIterationAsync("NonExistentLoop", 0).ConfigureAwait(false);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetNestedLoopsInForEachIterationAsync_Should_Return_Null_When_Loop_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetNestedLoopsInForEachIterationAsync("NonExistentLoop", 0).ConfigureAwait(false);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetNestedLoopsInUntilIterationAsync_Should_Return_Null_When_Loop_Does_Not_Exist()
    {
        var workflowRun = await CreateWorkflowRunAsync().ConfigureAwait(false);
        var navigator = new WorkflowRunNavigator(workflowRun);

        var result = await navigator.GetNestedLoopsInUntilIterationAsync("NonExistentLoop", 0).ConfigureAwait(false);

        Assert.That(result, Is.Null);
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
}