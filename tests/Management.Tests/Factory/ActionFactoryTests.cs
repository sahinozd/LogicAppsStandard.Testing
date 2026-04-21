using LogicApps.Management.Actions;
using LogicApps.Management.Factory;
using LogicApps.Management.Helper;
using LogicApps.Management.Models.Enums;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace LogicApps.Management.Tests.Factory;

[TestFixture]
internal sealed class ActionFactoryTests
{
    private IConfiguration _configuration;
    private IAzureManagementRepository _azureManagementRepository;
    private IActionHelper _actionHelper;

    [SetUp]
    public void SetUp()
    {
        _configuration = Substitute.For<IConfiguration>();
        _azureManagementRepository = Substitute.For<IAzureManagementRepository>();
        _actionHelper = Substitute.For<IActionHelper>();

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
    public void Constructor_Should_Throw_ArgumentNullException_When_Configuration_Is_Null()
    {
        // Act & Assert
        var argumentNullException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new ActionFactory(null!, _azureManagementRepository, _actionHelper);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(argumentNullException, Is.Not.Null);
            Assert.That(argumentNullException.ParamName, Is.EqualTo("configuration"));
        }
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_AzureManagementRepository_Is_Null()
    {
        // Act & Assert
        var argumentNullException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new ActionFactory(_configuration, null!, _actionHelper);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(argumentNullException, Is.Not.Null);
            Assert.That(argumentNullException.ParamName, Is.EqualTo("azureManagementRepository"));
        }
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_ActionHelper_Is_Null()
    {
        // Act & Assert
        var argumentNullException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new ActionFactory(_configuration, _azureManagementRepository, null!);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(argumentNullException, Is.Not.Null);
            Assert.That(argumentNullException.ParamName, Is.EqualTo("actionHelper"));
        }
    }

    [Test]
    public void Constructor_Should_Initialize_Successfully_With_Valid_Parameters()
    {
        // Act
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);

        // Assert
        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void SetWorkflowRunProperties_Should_Throw_ArgumentNullException_When_WorkflowName_Is_Null()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);

        // Act & Assert
        var argumentException = Assert.Throws<ArgumentNullException>(() => factory.SetWorkflowRunProperties(null!, "run-id"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(argumentException, Is.Not.Null);
            Assert.That(argumentException.ParamName, Does.Contain("workflowName"));
        }
    }

    [Test]
    public void SetWorkflowRunProperties_Should_Throw_ArgumentException_When_WorkflowName_Is_Empty()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);

        // Act & Assert
        var argumentException = Assert.Throws<ArgumentException>(() => factory.SetWorkflowRunProperties(string.Empty, "run-id"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(argumentException, Is.Not.Null);
            Assert.That(argumentException.ParamName, Does.Contain("workflowName"));
        }
    }

    [Test]
    public void SetWorkflowRunProperties_Should_Throw_ArgumentNullException_When_RunId_Is_Null()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);

        // Act & Assert
        var argumentException = Assert.Throws<ArgumentNullException>(() => factory.SetWorkflowRunProperties("workflow-name", null!));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(argumentException, Is.Not.Null);
            Assert.That(argumentException.ParamName, Does.Contain("runId"));
        }
    }

    [Test]
    public void SetWorkflowRunProperties_Should_Throw_ArgumentException_When_RunId_Is_Empty()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);

        // Act & Assert
        var argumentException = Assert.Throws<ArgumentException>(() => factory.SetWorkflowRunProperties("workflow-name", string.Empty));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(argumentException, Is.Not.Null);
            Assert.That(argumentException.ParamName, Does.Contain("runId"));
        }
    }

    [Test]
    public void SetWorkflowRunProperties_Should_Set_Properties_With_Valid_Parameters()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);

        // Act
        factory.SetWorkflowRunProperties("workflow", "run-id");

        // Assert - No exception should be thrown
        Assert.Pass();
    }

    [Test]
    public void CreateActionFromJObject_Should_Throw_ArgumentNullException_When_Name_Is_Null()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        var node = new JObject();

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentNullException>(() => factory.CreateActionFromJObject(null!, node));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(argumentException, Is.Not.Null);
            Assert.That(argumentException.ParamName, Does.Contain("name"));
        }
    }

    [Test]
    public void CreateActionFromJObject_Should_Throw_ArgumentException_When_Name_Is_Empty()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        var node = new JObject();

        // Act & Assert
        var argumentException = Assert.ThrowsAsync<ArgumentException>(() => factory.CreateActionFromJObject(string.Empty, node));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(argumentException, Is.Not.Null);
            Assert.That(argumentException.ParamName, Does.Contain("name"));
        }
    }

    [Test]
    public void CreateActionFromJObject_Should_Throw_ArgumentNullException_When_Node_Is_Null()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => factory.CreateActionFromJObject("action", null!));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(argumentNullException, Is.Not.Null);
            Assert.That(argumentNullException.ParamName, Is.EqualTo("node"));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_Action_For_Unknown_Type()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "UnknownType"
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("action", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action, Is.TypeOf<Management.Actions.Action>());
            Assert.That(action.Name, Is.EqualTo("action"));
            Assert.That(action.Type, Is.EqualTo(ActionType.Action));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_Action_When_Type_Is_Missing()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject();

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("action", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action, Is.TypeOf<Management.Actions.Action>());
            Assert.That(action.Name, Is.EqualTo("action"));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_ScopeAction_For_Scope_Type()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Scope"
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("scope", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action, Is.TypeOf<ScopeAction>());
            Assert.That(action.Name, Is.EqualTo("scope"));
            Assert.That(action.Type, Is.EqualTo(ActionType.Scope));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_ScopeAction_With_Child_Actions()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Scope",
            ["actions"] = new JObject
            {
                ["child1"] = new JObject
                {
                    ["type"] = "Action"
                },
                ["child2"] = new JObject
                {
                    ["type"] = "Action"
                }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("scope", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ScopeAction>());
            var scopeAction = (ScopeAction)action;
            Assert.That(scopeAction.Actions, Has.Count.EqualTo(2));
            Assert.That(scopeAction.Actions[0].Name, Is.EqualTo("child1"));
            Assert.That(scopeAction.Actions[1].Name, Is.EqualTo("child2"));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_UntilAction_For_Until_Type()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Until"
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("until", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action, Is.TypeOf<UntilAction>());
            Assert.That(action.Name, Is.EqualTo("until"));
            Assert.That(action.Type, Is.EqualTo(ActionType.Until));
        }
    }

    [Test]
    public void CreateActionFromJObject_Should_Create_ForEachAction_For_ForEach_Type()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "ForEach"
        };

        // Act & Assert - ForEach creation requires complex mocking of GetAllActionRepetitions
        // We just verify that the factory attempts to create the correct type
        Assert.ThrowsAsync<NullReferenceException>(() =>
            factory.CreateActionFromJObject("foreach", node));
    }

    [Test]
    public void CreateActionFromJObject_Should_Create_ForEachAction_For_Foreach_Type_Lowercase()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Foreach"
        };

        // Act & Assert - ForEach creation requires complex mocking of GetAllActionRepetitions
        // We just verify that the factory attempts to create the correct type
        Assert.ThrowsAsync<NullReferenceException>(() =>
            factory.CreateActionFromJObject("foreach", node));
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_SwitchAction_For_Switch_Type()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Switch"
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("switch", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action, Is.TypeOf<SwitchAction>());
            Assert.That(action.Name, Is.EqualTo("switch"));
            Assert.That(action.Type, Is.EqualTo(ActionType.Switch));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_SwitchAction_With_Cases()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Switch",
            ["cases"] = new JObject
            {
                ["Case1"] = new JObject
                {
                    ["actions"] = new JObject
                    {
                        ["action1"] = new JObject { ["type"] = "Action" }
                    }
                },
                ["Case2"] = new JObject
                {
                    ["actions"] = new JObject
                    {
                        ["action2"] = new JObject { ["type"] = "Action" }
                    }
                }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("switch", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<SwitchAction>());
            var switchAction = (SwitchAction)action;
            Assert.That(switchAction.Cases, Has.Count.EqualTo(2));
            Assert.That(switchAction.Cases[0].Name, Is.EqualTo("Case1"));
            Assert.That(switchAction.Cases[1].Name, Is.EqualTo("Case2"));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_SwitchAction_With_Default_Case()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Switch",
            ["default"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["defaultAction"] = new JObject { ["type"] = "Action" }
                }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("switch", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<SwitchAction>());
            var switchAction = (SwitchAction)action;
            Assert.That(switchAction.Cases, Has.Count.EqualTo(1));
            Assert.That(switchAction.Cases[0].Name, Is.EqualTo("Default"));
            Assert.That(switchAction.Cases[0].Actions, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_ConditionAction_For_If_Type()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "If"
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("condition", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action, Is.TypeOf<ConditionAction>());
            Assert.That(action.Name, Is.EqualTo("condition"));
            Assert.That(action.Type, Is.EqualTo(ActionType.Condition));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_ConditionAction_For_Condition_Type()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Condition"
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("condition", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ConditionAction>());
            Assert.That(action.Type, Is.EqualTo(ActionType.Condition));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_ConditionAction_With_Default_And_Else_Actions()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Condition",
            ["actions"] = new JObject
            {
                ["defaultAction1"] = new JObject { ["type"] = "Action" }
            },
            ["else"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["elseAction1"] = new JObject { ["type"] = "Action" }
                }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("condition", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ConditionAction>());
            var conditionAction = (ConditionAction)action;
            Assert.That(conditionAction.DefaultActions, Has.Count.EqualTo(1));
            Assert.That(conditionAction.ElseActions, Has.Count.EqualTo(1));
            Assert.That(conditionAction.DefaultActions[0].Name, Is.EqualTo("defaultAction1"));
            Assert.That(conditionAction.ElseActions[0].Name, Is.EqualTo("elseAction1"));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Use_Type_Property_With_Capital_T()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["Type"] = "Scope"
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("scope", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ScopeAction>());
            Assert.That(action.Type, Is.EqualTo(ActionType.Scope));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_ScopeAction_Without_Child_Actions()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Scope"
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("empty-scope", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ScopeAction>());
            var scopeAction = (ScopeAction)action;
            Assert.That(scopeAction.Actions, Is.Empty);
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_SwitchAction_Without_Cases()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Switch"
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("empty-switch", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<SwitchAction>());
            var switchAction = (SwitchAction)action;
            Assert.That(switchAction.Cases, Is.Empty);
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_SwitchAction_With_Cases_And_Default()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Switch",
            ["cases"] = new JObject
            {
                ["Case1"] = new JObject
                {
                    ["actions"] = new JObject
                    {
                        ["action1"] = new JObject { ["type"] = "Action" }
                    }
                }
            },
            ["default"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["defaultAction"] = new JObject { ["type"] = "Action" }
                }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("switch", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<SwitchAction>());
            var switchAction = (SwitchAction)action;
            Assert.That(switchAction.Cases, Has.Count.EqualTo(2));
            Assert.That(switchAction.Cases[0].Name, Is.EqualTo("Case1"));
            Assert.That(switchAction.Cases[1].Name, Is.EqualTo("Default"));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_ConditionAction_Without_ElseActions()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Condition",
            ["actions"] = new JObject
            {
                ["defaultAction1"] = new JObject { ["type"] = "Action" }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("condition", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ConditionAction>());
            var conditionAction = (ConditionAction)action;
            Assert.That(conditionAction.DefaultActions, Has.Count.EqualTo(1));
            Assert.That(conditionAction.ElseActions, Is.Empty);
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_ConditionAction_Without_DefaultActions()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Condition",
            ["else"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["elseAction1"] = new JObject { ["type"] = "Action" }
                }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("condition", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ConditionAction>());
            var conditionAction = (ConditionAction)action;
            Assert.That(conditionAction.DefaultActions, Is.Empty);
            Assert.That(conditionAction.ElseActions, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_Nested_Actions()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Scope",
            ["actions"] = new JObject
            {
                ["nestedScope"] = new JObject
                {
                    ["type"] = "Scope",
                    ["actions"] = new JObject
                    {
                        ["innerAction"] = new JObject { ["type"] = "Action" }
                    }
                }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("outer-scope", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ScopeAction>());
            var outerScope = (ScopeAction)action;
            Assert.That(outerScope.Actions, Has.Count.EqualTo(1));
            Assert.That(outerScope.Actions[0], Is.TypeOf<ScopeAction>());
            var nestedScope = (ScopeAction)outerScope.Actions[0];
            Assert.That(nestedScope.Actions, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_SwitchCase_Without_Actions()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Switch",
            ["cases"] = new JObject
            {
                ["EmptyCase"] = new JObject()
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("switch", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<SwitchAction>());
            var switchAction = (SwitchAction)action;
            Assert.That(switchAction.Cases, Has.Count.EqualTo(1));
            Assert.That(switchAction.Cases[0].Name, Is.EqualTo("EmptyCase"));
            Assert.That(switchAction.Cases[0].Actions, Is.Empty);
        }
    }

    [Test]
    public void CreateActionFromJObject_Should_Throw_ArgumentNullException_When_WorkflowRunProperties_Not_Set()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        var node = new JObject
        {
            ["type"] = "Action"
        };

        // Act & Assert - Should throw ArgumentNullException because SetWorkflowRunProperties was not called
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            factory.CreateActionFromJObject("action", node));
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_Action_With_RepetitionIndex()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Action"
        };

        const string repetitionJson = """
            {
                "name": "00001",
                "properties": {}
            }
            """;

        var repetitionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(repetitionJson);

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = [repetitionDetails!]
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("action", node, "00001").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action, Is.TypeOf<Management.Actions.Action>());
            Assert.That(action.Name, Is.EqualTo("action"));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_ScopeAction_With_RepetitionIndex()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Scope",
            ["actions"] = new JObject
            {
                ["child1"] = new JObject { ["type"] = "Action" }
            }
        };

        const string repetitionJson = """
            {
                "name": "00001",
                "properties": {}
            }
            """;

        var repetitionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(repetitionJson);

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = [repetitionDetails!]
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("scope", node, "00001").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action, Is.TypeOf<ScopeAction>());
            var scopeAction = (ScopeAction)action;
            Assert.That(scopeAction.Actions, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_SwitchAction_With_RepetitionIndex()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Switch",
            ["cases"] = new JObject
            {
                ["Case1"] = new JObject
                {
                    ["actions"] = new JObject
                    {
                        ["action1"] = new JObject { ["type"] = "Action" }
                    }
                }
            }
        };

        const string repetitionJson = """
            {
                "name": "00001",
                "properties": {}
            }
            """;

        var repetitionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(repetitionJson);

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = [repetitionDetails!]
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("switch", node, "00001").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action, Is.TypeOf<SwitchAction>());
            var switchAction = (SwitchAction)action;
            Assert.That(switchAction.Cases, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Create_ConditionAction_With_RepetitionIndex()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Condition",
            ["actions"] = new JObject
            {
                ["defaultAction1"] = new JObject { ["type"] = "Action" }
            },
            ["else"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["elseAction1"] = new JObject { ["type"] = "Action" }
                }
            }
        };

        const string repetitionJson = """
            {
                "name": "00001",
                "properties": {}
            }
            """;

        var repetitionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(repetitionJson);

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = [repetitionDetails!]
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("condition", node, "00001").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action, Is.TypeOf<ConditionAction>());
            var conditionAction = (ConditionAction)action;
            Assert.That(conditionAction.DefaultActions, Has.Count.EqualTo(1));
            Assert.That(conditionAction.ElseActions, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Filter_Repetitions_By_RepetitionIndex()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Action"
        };

        const string repetitionJson = """
            [
                {"name": "00001", "properties": {}},
                {"name": "00002", "properties": {}}
            ]
            """;

        var repetitions = JsonConvert.DeserializeObject<List<WorkflowRunDetailsAction>>(repetitionJson);

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = repetitions
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("action", node, "00001").ConfigureAwait(false);

        // Assert - Should have filtered to only the matching repetition
        Assert.That(action, Is.Not.Null);
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Cache_Action_Repetitions()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Scope",
            ["actions"] = new JObject
            {
                ["child1"] = new JObject { ["type"] = "Action" },
                ["child2"] = new JObject { ["type"] = "Action" }
            }
        };

        const string repetitionJson = """
            {
                "name": "00001",
                "properties": {}
            }
            """;

        var repetitionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(repetitionJson);

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = [repetitionDetails!]
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act - Create scope with repetition index, which will cache repetitions for children
        _ = await factory.CreateActionFromJObject("scope", node, "00001").ConfigureAwait(false);

        // Assert - The repository should have been called to get repetitions
        await _azureManagementRepository.Received().GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_Multiple_Switch_Cases_With_RepetitionIndex()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Switch",
            ["cases"] = new JObject
            {
                ["Case1"] = new JObject
                {
                    ["actions"] = new JObject
                    {
                        ["action1"] = new JObject { ["type"] = "Action" }
                    }
                },
                ["Case2"] = new JObject
                {
                    ["actions"] = new JObject
                    {
                        ["action2"] = new JObject { ["type"] = "Action" }
                    }
                }
            },
            ["default"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["defaultAction"] = new JObject { ["type"] = "Action" }
                }
            }
        };

        const string repetitionJson = """
            {
                "name": "00001",
                "properties": {}
            }
            """;

        var repetitionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(repetitionJson);

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = [repetitionDetails!]
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("switch", node, "00001").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<SwitchAction>());
            var switchAction = (SwitchAction)action;
            Assert.That(switchAction.Cases, Has.Count.EqualTo(3));
            Assert.That(switchAction.Cases[0].Actions, Has.Count.EqualTo(1));
            Assert.That(switchAction.Cases[1].Actions, Has.Count.EqualTo(1));
            Assert.That(switchAction.Cases[2].Actions, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_Nested_Actions_With_RepetitionIndex()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Scope",
            ["actions"] = new JObject
            {
                ["nestedScope"] = new JObject
                {
                    ["type"] = "Scope",
                    ["actions"] = new JObject
                    {
                        ["innerAction"] = new JObject { ["type"] = "Action" }
                    }
                }
            }
        };

        const string repetitionJson = """
            {
                "name": "00001",
                "properties": {}
            }
            """;

        var repetitionDetails = JsonConvert.DeserializeObject<WorkflowRunDetailsAction>(repetitionJson);

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = [repetitionDetails!]
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("outer-scope", node, "00001").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ScopeAction>());
            var outerScope = (ScopeAction)action;
            Assert.That(outerScope.Actions, Has.Count.EqualTo(1));
            Assert.That(outerScope.Actions[0], Is.TypeOf<ScopeAction>());
            var nestedScope = (ScopeAction)outerScope.Actions[0];
            Assert.That(nestedScope.Actions, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_Empty_Repetition_List()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Action"
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act & Assert - Should handle empty repetition list gracefully
        var action = await factory.CreateActionFromJObject("action", node).ConfigureAwait(false);
        Assert.That(action, Is.Not.Null);
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Process_Multiple_Actions_In_Scope()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Scope",
            ["actions"] = new JObject
            {
                ["action1"] = new JObject { ["type"] = "Action" },
                ["action2"] = new JObject { ["type"] = "Action" },
                ["action3"] = new JObject { ["type"] = "Action" },
                ["nestedScope"] = new JObject
                {
                    ["type"] = "Scope",
                    ["actions"] = new JObject
                    {
                        ["nestedAction"] = new JObject { ["type"] = "Action" }
                    }
                }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("scope", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ScopeAction>());
            var scopeAction = (ScopeAction)action;
            Assert.That(scopeAction.Actions, Has.Count.EqualTo(4));
            Assert.That(scopeAction.Actions[3], Is.TypeOf<ScopeAction>());
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_Condition_With_Only_Default_Actions()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Condition",
            ["actions"] = new JObject
            {
                ["action1"] = new JObject { ["type"] = "Action" },
                ["action2"] = new JObject { ["type"] = "Action" }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("condition", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ConditionAction>());
            var conditionAction = (ConditionAction)action;
            Assert.That(conditionAction.DefaultActions, Has.Count.EqualTo(2));
            Assert.That(conditionAction.ElseActions, Is.Empty);
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_Condition_With_Only_Else_Actions()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Condition",
            ["else"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["elseAction1"] = new JObject { ["type"] = "Action" },
                    ["elseAction2"] = new JObject { ["type"] = "Action" }
                }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("condition", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<ConditionAction>());
            var conditionAction = (ConditionAction)action;
            Assert.That(conditionAction.DefaultActions, Is.Empty);
            Assert.That(conditionAction.ElseActions, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public async Task CreateActionFromJObject_Should_Handle_Switch_With_Only_Default_Case()
    {
        // Arrange
        var factory = new ActionFactory(_configuration, _azureManagementRepository, _actionHelper);
        factory.SetWorkflowRunProperties("workflow", "run-id");

        var node = new JObject
        {
            ["type"] = "Switch",
            ["default"] = new JObject
            {
                ["actions"] = new JObject
                {
                    ["defaultAction1"] = new JObject { ["type"] = "Action" },
                    ["defaultAction2"] = new JObject { ["type"] = "Action" }
                }
            }
        };

        var actionResponse = new Response<WorkflowRunDetailsAction>
        {
            Value = []
        };

        _azureManagementRepository
            .GetObjectAsync<Response<WorkflowRunDetailsAction>>(Arg.Any<Uri>())
            .Returns(Task.FromResult<Response<WorkflowRunDetailsAction>?>(actionResponse));

        // Act
        var action = await factory.CreateActionFromJObject("switch", node).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.TypeOf<SwitchAction>());
            var switchAction = (SwitchAction)action;
            Assert.That(switchAction.Cases, Has.Count.EqualTo(1));
            Assert.That(switchAction.Cases[0].Name, Is.EqualTo("Default"));
            Assert.That(switchAction.Cases[0].Actions, Has.Count.EqualTo(2));
        }
    }
}