using LogicApps.Management.Actions;
using LogicApps.Management.Models.Enums;
using NUnit.Framework;

namespace LogicApps.Management.Tests.Actions;


[TestFixture]
internal sealed class ScopeActionTests
{
    [Test]
    public void Constructor_Should_Initialize_Properties_When_Valid_Parameters_Are_Provided()
    {
        // Arrange
        const string actionName = "TestScope";
        const ActionType actionType = ActionType.Scope;

        // Act
        var scopeAction = new ScopeAction(actionName, actionType);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(scopeAction, Is.Not.Null);
            Assert.That(scopeAction.Name, Is.EqualTo(actionName));
            Assert.That(scopeAction.Type, Is.EqualTo(actionType));
            Assert.That(scopeAction.Actions, Is.Not.Null);
            Assert.That(scopeAction.Actions, Is.Empty);
        }
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_Name_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new ScopeAction(null!, ActionType.Scope);
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
            _ = new ScopeAction(string.Empty, ActionType.Scope);
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
            _ = new ScopeAction("TestScope", (ActionType)999);
        });

        // Assert
        Assert.That(argumentOutOfRangeException?.ParamName, Is.EqualTo("actionType"));
    }

    [Test]
    public void Actions_Property_Should_Be_Modifiable()
    {
        // Arrange
        var scopeAction = new ScopeAction("Try", ActionType.Scope);
        var childAction = new LogicApps.Management.Actions.Action("Action", ActionType.Action);

        // Act
        scopeAction.Actions.Add(childAction);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(scopeAction.Actions, Has.Count.EqualTo(1));
            Assert.That(scopeAction.Actions[0], Is.EqualTo(childAction));
            Assert.That(scopeAction.Actions[0].Name, Is.EqualTo("Action"));
        }
    }
}