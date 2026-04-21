using LogicApps.Management.Actions;
using LogicApps.Management.Models.Enums;
using NUnit.Framework;

namespace LogicApps.Management.Tests.Actions;

[TestFixture]
internal sealed class ConditionActionTests
{
    [Test]
    public void Constructor_Should_Initialize_Properties_When_Valid_Parameters_Are_Provided()
    {
        // Arrange
        const string actionName = "TestCondition";
        const ActionType actionType = ActionType.Condition;

        // Act
        var conditionAction = new ConditionAction(actionName, actionType);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(conditionAction, Is.Not.Null);
            Assert.That(conditionAction.Name, Is.EqualTo(actionName));
            Assert.That(conditionAction.Type, Is.EqualTo(actionType));
            Assert.That(conditionAction.ElseActions, Is.Not.Null);
            Assert.That(conditionAction.ElseActions, Is.Empty);
            Assert.That(conditionAction.DefaultActions, Is.Not.Null);
            Assert.That(conditionAction.DefaultActions, Is.Empty);
        }
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_Name_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new ConditionAction(null!, ActionType.Condition);
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
            _ = new ConditionAction(string.Empty, ActionType.Condition);
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
            _ = new ConditionAction("TestCondition", (ActionType)999);
        });

        // Assert
        Assert.That(argumentOutOfRangeException?.ParamName, Is.EqualTo("actionType"));
    }

    [Test]
    public void ElseActions_Property_Should_Be_Modifiable()
    {
        // Arrange
        var conditionAction = new ConditionAction("TestCondition", ActionType.Condition);
        var elseAction = new LogicApps.Management.Actions.Action("ElseAction", ActionType.Action);

        // Act
        conditionAction.ElseActions.Add(elseAction);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(conditionAction.ElseActions, Has.Count.EqualTo(1));
            Assert.That(conditionAction.ElseActions[0], Is.EqualTo(elseAction));
            Assert.That(conditionAction.ElseActions[0].Name, Is.EqualTo("ElseAction"));
        }
    }

    [Test]
    public void DefaultActions_Property_Should_Be_Modifiable()
    {
        // Arrange
        var conditionAction = new ConditionAction("TestCondition", ActionType.Condition);
        var defaultAction = new LogicApps.Management.Actions.Action("DefaultAction", ActionType.Action);

        // Act
        conditionAction.DefaultActions.Add(defaultAction);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(conditionAction.DefaultActions, Has.Count.EqualTo(1));
            Assert.That(conditionAction.DefaultActions[0], Is.EqualTo(defaultAction));
            Assert.That(conditionAction.DefaultActions[0].Name, Is.EqualTo("DefaultAction"));
        }
    }

    [Test]
    public void Both_ElseActions_And_DefaultActions_Should_Be_Independent()
    {
        // Arrange
        var conditionAction = new ConditionAction("TestCondition", ActionType.Condition);
        var elseAction = new LogicApps.Management.Actions.Action("ElseAction", ActionType.Action);
        var defaultAction = new LogicApps.Management.Actions.Action("DefaultAction", ActionType.Action);

        // Act
        conditionAction.ElseActions.Add(elseAction);
        conditionAction.DefaultActions.Add(defaultAction);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(conditionAction.ElseActions, Has.Count.EqualTo(1));
            Assert.That(conditionAction.DefaultActions, Has.Count.EqualTo(1));
            Assert.That(conditionAction.ElseActions[0].Name, Is.EqualTo("ElseAction"));
            Assert.That(conditionAction.DefaultActions[0].Name, Is.EqualTo("DefaultAction"));
        }
    }
}