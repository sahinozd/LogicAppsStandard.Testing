using LogicApps.Management.Models.Enums;
using NUnit.Framework;

namespace LogicApps.Management.Tests.Actions;

[TestFixture]
internal sealed class ActionTests
{
    [Test]
    public void Constructor_Should_Initialize_Properties_When_Valid_Parameters_Are_Provided()
    {
        // Arrange
        const string actionName = "TestAction";
        const ActionType actionType = ActionType.Action;

        // Act
        var action = new LogicApps.Management.Actions.Action(actionName, actionType);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(action.Name, Is.EqualTo(actionName));
            Assert.That(action.Type, Is.EqualTo(actionType));
        }
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_Name_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new LogicApps.Management.Actions.Action(null!, ActionType.Action);
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
            _ = new LogicApps.Management.Actions.Action(string.Empty, ActionType.Action);
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
            _ = new LogicApps.Management.Actions.Action("TestAction", (ActionType)999);
        });

        // Assert
        Assert.That(argumentOutOfRangeException?.ParamName, Is.EqualTo("actionType"));
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_With_Different_ActionTypes()
    {
        // Arrange & Act
        var scopeAction = new LogicApps.Management.Actions.Action("ScopeTest", ActionType.Scope);
        var untilAction = new LogicApps.Management.Actions.Action("UntilTest", ActionType.Until);
        var forEachAction = new LogicApps.Management.Actions.Action("ForEachTest", ActionType.ForEach);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(scopeAction.Type, Is.EqualTo(ActionType.Scope));
            Assert.That(untilAction.Type, Is.EqualTo(ActionType.Until));
            Assert.That(forEachAction.Type, Is.EqualTo(ActionType.ForEach));
        }
    }
}