using LogicApps.Management.Actions;
using NUnit.Framework;

namespace LogicApps.Management.Tests.Actions;

[TestFixture]
internal sealed class SwitchCaseTests
{
    [Test]
    public void Constructor_Should_Initialize_Properties_When_Created()
    {
        // Arrange & Act
        var switchCase = new SwitchCase
        {
            Name = "Case1"
        };

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(switchCase, Is.Not.Null);
            Assert.That(switchCase.Name, Is.EqualTo("Case1"));
            Assert.That(switchCase.Actions, Is.Not.Null);
            Assert.That(switchCase.Actions, Is.Empty);
        }
    }

    [Test]
    public void Name_Property_Should_Be_Required_And_Settable()
    {
        // Arrange
        var switchCase = new SwitchCase
        {
            Name = "InitialName"
        };

        // Act
        switchCase.Name = "UpdatedName";

        // Assert
        Assert.That(switchCase.Name, Is.EqualTo("UpdatedName"));
    }

    [Test]
    public void Actions_Property_Should_Be_Modifiable()
    {
        // Arrange
        var switchCase = new SwitchCase
        {
            Name = "Case1"
        };
        var action = new LogicApps.Management.Actions.Action("SwitchBranch", Models.Enums.ActionType.Action);

        // Act
        switchCase.Actions.Add(action);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(switchCase.Actions, Has.Count.EqualTo(1));
            Assert.That(switchCase.Actions[0], Is.EqualTo(action));
            Assert.That(switchCase.Actions[0].Name, Is.EqualTo("SwitchBranch"));
        }
    }

    [Test]
    public void Multiple_Actions_Should_Be_Added_To_Actions_List()
    {
        // Arrange
        var switchCase = new SwitchCase
        {
            Name = "Case1"
        };
        var action1 = new LogicApps.Management.Actions.Action("Action1", Models.Enums.ActionType.Action);
        var action2 = new LogicApps.Management.Actions.Action("Action2", Models.Enums.ActionType.Action);
        var action3 = new LogicApps.Management.Actions.Action("Action3", Models.Enums.ActionType.Action);

        // Act
        switchCase.Actions.Add(action1);
        switchCase.Actions.Add(action2);
        switchCase.Actions.Add(action3);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(switchCase.Actions, Has.Count.EqualTo(3));
            Assert.That(switchCase.Actions[0].Name, Is.EqualTo("Action1"));
            Assert.That(switchCase.Actions[1].Name, Is.EqualTo("Action2"));
            Assert.That(switchCase.Actions[2].Name, Is.EqualTo("Action3"));
        }
    }
}