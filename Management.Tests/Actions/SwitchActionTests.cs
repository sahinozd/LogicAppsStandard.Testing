using LogicApps.Management.Actions;
using LogicApps.Management.Models.Enums;
using NUnit.Framework;

namespace LogicApps.Management.Tests.Actions;

[TestFixture]
internal sealed class SwitchActionTests
{
    [Test]
    public void Constructor_Should_Initialize_Properties_When_Valid_Parameters_Are_Provided()
    {
        // Arrange
        const string actionName = "Check_a_condition";
        const ActionType actionType = ActionType.Switch;

        // Act
        var switchAction = new SwitchAction(actionName, actionType);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(switchAction, Is.Not.Null);
            Assert.That(switchAction.Name, Is.EqualTo(actionName));
            Assert.That(switchAction.Type, Is.EqualTo(actionType));
            Assert.That(switchAction.Cases, Is.Not.Null);
            Assert.That(switchAction.Cases, Is.Empty);
        }
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_Name_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new SwitchAction(null!, ActionType.Switch);
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
            _ = new SwitchAction(string.Empty, ActionType.Switch);
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
            _ = new SwitchAction("SwitchBranch", (ActionType)999);
        });

        // Assert
        Assert.That(argumentOutOfRangeException?.ParamName, Is.EqualTo("actionType"));
    }

    [Test]
    public void Cases_Property_Should_Be_Modifiable()
    {
        // Arrange
        var switchAction = new SwitchAction("SwitchBranch", ActionType.Switch);
        var switchCase = new SwitchCase
        {
            Name = "Case1"
        };

        // Act
        switchAction.Cases.Add(switchCase);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(switchAction.Cases, Has.Count.EqualTo(1));
            Assert.That(switchAction.Cases[0], Is.EqualTo(switchCase));
            Assert.That(switchAction.Cases[0].Name, Is.EqualTo("Case1"));
        }
    }
}