using LogicApps.Management.Actions;
using LogicApps.Management.Models.Enums;
using NUnit.Framework;

namespace LogicApps.TestFramework.Specifications.Tests;

[TestFixture]
public class ActionPathNavigatorTests
{
    [Test]
    public void NavigateToPath_WithNullActions_ThrowsArgumentNullException()
    {
        // Arrange
        IList<BaseAction> actions = null!;
        const string path = "Test";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ActionPathNavigator.NavigateToPath(actions, path));
    }

    [Test]
    public void NavigateToPath_WithNullPath_ThrowsArgumentNullException()
    {
        // Arrange
        IList<BaseAction> actions = [];
        string path = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ActionPathNavigator.NavigateToPath(actions, path));
    }

    [Test]
    public void NavigateToPath_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        IList<BaseAction> actions = [];
        const string path = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ActionPathNavigator.NavigateToPath(actions, path));
    }

    [Test]
    public void NavigateToPath_WithWhitespacePath_ThrowsArgumentException()
    {
        // Arrange
        IList<BaseAction> actions = [];
        const string path = "   ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ActionPathNavigator.NavigateToPath(actions, path));
    }

    [Test]
    public void NavigateToPath_WithSimpleActionName_ReturnsChildActions()
    {
        // Arrange
        var childAction1 = new Management.Actions.Action("Child1", ActionType.Action);
        var childAction2 = new Management.Actions.Action("Child2", ActionType.Action);
        
        var scopeAction = new ScopeAction("TestScope", ActionType.Scope);
        scopeAction.Actions.Add(childAction1);
        scopeAction.Actions.Add(childAction2);

        IList<BaseAction> actions = [scopeAction];
        const string path = "TestScope";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Contains.Item(childAction1));
        Assert.That(result, Contains.Item(childAction2));
    }

    [Test]
    public void NavigateToPath_WithNonExistentAction_ReturnsEmpty()
    {
        // Arrange
        var scopeAction = new ScopeAction("TestScope", ActionType.Scope);
        IList<BaseAction> actions = [scopeAction];
        const string path = "NonExistent";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NavigateToPath_WithNestedPath_ReturnsNestedActions()
    {
        // Arrange
        var deepAction = new Management.Actions.Action("DeepAction", ActionType.Action);
        
        var innerScope = new ScopeAction("InnerScope", ActionType.Scope);
        innerScope.Actions.Add(deepAction);
        
        var outerScope = new ScopeAction("OuterScope", ActionType.Scope);
        outerScope.Actions.Add(innerScope);

        IList<BaseAction> actions = [outerScope];
        const string path = "OuterScope.InnerScope";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(deepAction));
    }

    [Test]
    public void NavigateToPath_WithConditionActionsPath_ReturnsDefaultActions()
    {
        // Arrange
        var defaultAction = new Management.Actions.Action("DefaultAction", ActionType.Action);
        var elseAction = new Management.Actions.Action("ElseAction", ActionType.Action);
        
        var condition = new ConditionAction("TestCondition", ActionType.Condition);
        condition.DefaultActions.Add(defaultAction);
        condition.ElseActions.Add(elseAction);

        var scope = new ScopeAction("TestScope", ActionType.Scope);
        scope.Actions.Add(condition);

        IList<BaseAction> actions = [scope];
        const string path = "TestScope.TestCondition.actions";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(defaultAction));
    }

    [Test]
    public void NavigateToPath_WithConditionElsePath_ReturnsElseActions()
    {
        // Arrange
        var defaultAction = new Management.Actions.Action("DefaultAction", ActionType.Action);
        var elseAction = new Management.Actions.Action("ElseAction", ActionType.Action);
        
        var condition = new ConditionAction("TestCondition", ActionType.Condition);
        condition.DefaultActions.Add(defaultAction);
        condition.ElseActions.Add(elseAction);

        var scope = new ScopeAction("TestScope", ActionType.Scope);
        scope.Actions.Add(condition);

        IList<BaseAction> actions = [scope];
        const string path = "TestScope.TestCondition.else";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(elseAction));
    }

    [Test]
    public void NavigateToPath_CaseInsensitiveActionName_FindsAction()
    {
        // Arrange
        var childAction = new Management.Actions.Action("Child", ActionType.Action) { DesignerName = "TestAction" };
        var scopeAction = new ScopeAction("TestScope", ActionType.Scope);
        scopeAction.Actions.Add(childAction);

        IList<BaseAction> actions = [scopeAction];
        const string path = "testscope"; // lowercase

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public void NavigateToPath_WithDesignerName_FindsActionByDesignerName()
    {
        // Arrange
        var childAction = new Management.Actions.Action("InternalName", ActionType.Action) { DesignerName = "Display Name" };
        var scopeAction = new ScopeAction("TestScope", ActionType.Scope) { DesignerName = "Test Scope" };
        scopeAction.Actions.Add(childAction);

        IList<BaseAction> actions = [scopeAction];
        const string path = "Test Scope"; // Using designer name with space

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(childAction));
    }

    [Test]
    public void NavigateToPath_WithMultipleSegments_NavigatesCorrectly()
    {
        // Arrange
        var finalAction = new Management.Actions.Action("FinalAction", ActionType.Action);
        
        var level3 = new ScopeAction("Level3", ActionType.Scope);
        level3.Actions.Add(finalAction);
        
        var level2 = new ScopeAction("Level2", ActionType.Scope);
        level2.Actions.Add(level3);
        
        var level1 = new ScopeAction("Level1", ActionType.Scope);
        level1.Actions.Add(level2);

        IList<BaseAction> actions = [level1];
        const string path = "Level1.Level2.Level3";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(finalAction));
    }

    [Test]
    public void NavigateToPath_WithPartialInvalidPath_ReturnsEmpty()
    {
        // Arrange
        var innerScope = new ScopeAction("InnerScope", ActionType.Scope);
        var outerScope = new ScopeAction("OuterScope", ActionType.Scope);
        outerScope.Actions.Add(innerScope);

        IList<BaseAction> actions = [outerScope];
        const string path = "OuterScope.NonExistent.InnerScope";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NavigateToPath_WithEmptyActions_ReturnsEmpty()
    {
        // Arrange
        IList<BaseAction> actions = [];
        const string path = "AnyPath";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NavigateToPath_WithActionHavingNoChildren_ReturnsEmpty()
    {
        // Arrange
        var simpleAction = new Management.Actions.Action("SimpleAction", ActionType.Action);
        IList<BaseAction> actions = [simpleAction];
        const string path = "SimpleAction";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NavigateToPath_WithSwitchCase_ReturnsCorrectCaseActions()
    {
        // Arrange
        var premiumAction = new Management.Actions.Action("PremiumAction", ActionType.Action);
        var standardAction = new Management.Actions.Action("StandardAction", ActionType.Action);

        var premiumCase = new SwitchCase { Name = "Premium" };
        premiumCase.Actions.Add(premiumAction);

        var standardCase = new SwitchCase { Name = "Standard" };
        standardCase.Actions.Add(standardAction);

        var switchAction = new SwitchAction("RouteByTier", ActionType.Switch);
        switchAction.Cases.Add(premiumCase);
        switchAction.Cases.Add(standardCase);

        IList<BaseAction> actions = [switchAction];
        const string path = "RouteByTier.Premium";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(premiumAction));
    }

    [Test]
    public void NavigateToPath_WithSwitchDefaultCase_ReturnsDefaultActions()
    {
        // Arrange
        var defaultAction = new Management.Actions.Action("DefaultAction", ActionType.Action);

        var defaultCase = new SwitchCase { Name = "Default" };
        defaultCase.Actions.Add(defaultAction);

        var switchAction = new SwitchAction("RouteByType", ActionType.Switch);
        switchAction.Cases.Add(defaultCase);

        IList<BaseAction> actions = [switchAction];
        const string path = "RouteByType.Default";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(defaultAction));
    }

    [Test]
    public void NavigateToPath_WithInvalidSwitchCase_ReturnsEmpty()
    {
        // Arrange
        var premiumCase = new SwitchCase { Name = "Premium" };
        premiumCase.Actions.Add(new Management.Actions.Action("Action", ActionType.Action));

        var switchAction = new SwitchAction("RouteByTier", ActionType.Switch);
        switchAction.Cases.Add(premiumCase);

        IList<BaseAction> actions = [switchAction];
        const string path = "RouteByTier.NonExistentCase";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NavigateToPath_WithSwitchCaseInsensitive_FindsCase()
    {
        // Arrange
        var premiumAction = new Management.Actions.Action("PremiumAction", ActionType.Action);

        var premiumCase = new SwitchCase { Name = "Premium" };
        premiumCase.Actions.Add(premiumAction);

        var switchAction = new SwitchAction("RouteByTier", ActionType.Switch);
        switchAction.Cases.Add(premiumCase);

        IList<BaseAction> actions = [switchAction];
        const string path = "RouteByTier.premium"; // lowercase

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(premiumAction));
    }

    [Test]
    public void NavigateToPath_WithSwitchInScope_ReturnsCorrectActions()
    {
        // Arrange
        var caseAction = new Management.Actions.Action("CaseAction", ActionType.Action);

        var switchCase = new SwitchCase { Name = "Case1" };
        switchCase.Actions.Add(caseAction);

        var switchAction = new SwitchAction("MySwitch", ActionType.Switch);
        switchAction.Cases.Add(switchCase);

        var scope = new ScopeAction("ProcessScope", ActionType.Scope);
        scope.Actions.Add(switchAction);

        IList<BaseAction> actions = [scope];
        const string path = "ProcessScope.MySwitch.Case1";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(caseAction));
    }

    [Test]
    public void NavigateToPath_WithMultipleSwitchCases_ReturnsCorrectCase()
    {
        // Arrange
        var case1Action = new Management.Actions.Action("Case1Action", ActionType.Action);
        var case2Action = new Management.Actions.Action("Case2Action", ActionType.Action);
        var case3Action = new Management.Actions.Action("Case3Action", ActionType.Action);

        var case1 = new SwitchCase { Name = "Case1" };
        case1.Actions.Add(case1Action);

        var case2 = new SwitchCase { Name = "Case2" };
        case2.Actions.Add(case2Action);

        var case3 = new SwitchCase { Name = "Case3" };
        case3.Actions.Add(case3Action);

        var switchAction = new SwitchAction("MultiSwitch", ActionType.Switch);
        switchAction.Cases.Add(case1);
        switchAction.Cases.Add(case2);
        switchAction.Cases.Add(case3);

        IList<BaseAction> actions = [switchAction];
        const string path = "MultiSwitch.Case2";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(case2Action));
    }

    [Test]
    public void NavigateToPath_WithEmptySwitchCase_ReturnsEmpty()
    {
        // Arrange
        var emptyCase = new SwitchCase { Name = "EmptyCase" };
        // No actions added

        var switchAction = new SwitchAction("MySwitch", ActionType.Switch);
        switchAction.Cases.Add(emptyCase);

        IList<BaseAction> actions = [switchAction];
        const string path = "MySwitch.EmptyCase";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NavigateToPath_WithSwitchInScopeNested_ReturnsCorrectActions()
    {
        // Arrange
        var caseAction = new Management.Actions.Action("HandleCase", ActionType.Action);

        var switchCase = new SwitchCase { Name = "Priority1" };
        switchCase.Actions.Add(caseAction);

        var switchAction = new SwitchAction("SwitchPriority", ActionType.Switch);
        switchAction.Cases.Add(switchCase);

        var scope = new ScopeAction("ProcessScope", ActionType.Scope);
        scope.Actions.Add(switchAction);

        IList<BaseAction> actions = [scope];
        const string path = "ProcessScope.SwitchPriority.Priority1";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(caseAction));
    }

    [Test]
    public void NavigateToPath_WithSwitchInCondition_ReturnsCorrectActions()
    {
        // Arrange
        var switchCaseAction = new Management.Actions.Action("HandleType", ActionType.Action);

        var switchCase = new SwitchCase { Name = "TypeA" };
        switchCase.Actions.Add(switchCaseAction);

        var switchAction = new SwitchAction("RouteByType", ActionType.Switch);
        switchAction.Cases.Add(switchCase);

        var condition = new ConditionAction("CheckCondition", ActionType.Condition);
        condition.DefaultActions.Add(switchAction);

        var scope = new ScopeAction("ProcessScope", ActionType.Scope);
        scope.Actions.Add(condition);

        IList<BaseAction> actions = [scope];
        const string path = "ProcessScope.CheckCondition.actions.RouteByType.TypeA";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(switchCaseAction));
    }

    [Test]
    public void NavigateToPath_WithNestedSwitchInScopeCondition_ReturnsCorrectAction()
    {
        // Arrange: Scope → Condition → Switch → Action
        var finalAction = new Management.Actions.Action("FinalAction", ActionType.Action);

        var switchCase = new SwitchCase { Name = "Case1" };
        switchCase.Actions.Add(finalAction);

        var switchAction = new SwitchAction("Switch", ActionType.Switch);
        switchAction.Cases.Add(switchCase);

        var condition = new ConditionAction("Condition", ActionType.Condition);
        condition.DefaultActions.Add(switchAction);

        var scope = new ScopeAction("Scope", ActionType.Scope);
        scope.Actions.Add(condition);

        IList<BaseAction> actions = [scope];
        const string path = "Scope.Condition.actions.Switch.Case1";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(finalAction));
    }

    [Test]
    public void NavigateToPath_WithMultipleActionsReturned_ReturnsAllActions()
    {
        // Arrange
        var action1 = new Management.Actions.Action("Action1", ActionType.Action);
        var action2 = new Management.Actions.Action("Action2", ActionType.Action);
        var action3 = new Management.Actions.Action("Action3", ActionType.Action);

        var scope = new ScopeAction("MultipleActions", ActionType.Scope);
        scope.Actions.Add(action1);
        scope.Actions.Add(action2);
        scope.Actions.Add(action3);

        IList<BaseAction> actions = [scope];
        const string path = "MultipleActions";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result, Contains.Item(action1));
        Assert.That(result, Contains.Item(action2));
        Assert.That(result, Contains.Item(action3));
    }

    [Test]
    public void NavigateToPath_WithConditionBothBranches_CanAccessBoth()
    {
        // Arrange
        var trueAction = new Management.Actions.Action("TrueAction", ActionType.Action);
        var falseAction = new Management.Actions.Action("FalseAction", ActionType.Action);

        var condition = new ConditionAction("MyCondition", ActionType.Condition);
        condition.DefaultActions.Add(trueAction);
        condition.ElseActions.Add(falseAction);

        IList<BaseAction> actions = [condition];

        // Act - Access TRUE branch
        var trueResult = ActionPathNavigator.NavigateToPath(actions, "MyCondition.actions");

        // Act - Access FALSE branch
        var falseResult = ActionPathNavigator.NavigateToPath(actions, "MyCondition.else");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trueResult, Has.Count.EqualTo(1));
            Assert.That(trueResult[0], Is.EqualTo(trueAction));

            Assert.That(falseResult, Has.Count.EqualTo(1));
            Assert.That(falseResult[0], Is.EqualTo(falseAction));
        }
    }

    [Test]
    public void NavigateToPath_WithSwitchNoMatchingActionFirst_ChecksForSwitchCase()
    {
        // Arrange
        var caseAction = new Management.Actions.Action("CaseAction", ActionType.Action);

        var switchCase = new SwitchCase { Name = "MatchingCase" };
        switchCase.Actions.Add(caseAction);

        var switchAction = new SwitchAction("MySwitch", ActionType.Switch);
        switchAction.Cases.Add(switchCase);

        IList<BaseAction> actions = [switchAction];
        const string path = "MySwitch.MatchingCase";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(caseAction));
    }

    [Test]
    public void NavigateToPath_WithActionNotLoopButIndexProvided_ReturnsEmpty()
    {
        // Arrange
        var scope = new ScopeAction("NotALoop", ActionType.Scope);
        scope.Actions.Add(new Management.Actions.Action("Child", ActionType.Action));

        IList<BaseAction> actions = [scope];
        const string path = "NotALoop[1]"; // Index on non-loop action

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NavigateToPath_WithSwitchAndConditionKeywords_PrefersCondition()
    {
        // Arrange - Test that "actions" and "else" are recognized as condition keywords even with switch present
        var conditionAction = new Management.Actions.Action("ConditionAction", ActionType.Action);

        var condition = new ConditionAction("MyCondition", ActionType.Condition);
        condition.DefaultActions.Add(conditionAction);

        var switchCase = new SwitchCase { Name = "Case1" };
        switchCase.Actions.Add(condition);

        var switchAction = new SwitchAction("MySwitch", ActionType.Switch);
        switchAction.Cases.Add(switchCase);

        IList<BaseAction> actions = [switchAction];
        const string path = "MySwitch.Case1.MyCondition.actions";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(conditionAction));
    }

    [Test]
    public void NavigateToPath_WithPathEndingInActions_ReturnsConditionDefaultBranch()
    {
        // Arrange
        var action1 = new Management.Actions.Action("Action1", ActionType.Action);
        var action2 = new Management.Actions.Action("Action2", ActionType.Action);

        var condition = new ConditionAction("Check", ActionType.Condition);
        condition.DefaultActions.Add(action1);
        condition.DefaultActions.Add(action2);

        IList<BaseAction> actions = [condition];
        const string path = "Check.actions";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void NavigateToPath_WithPathEndingInElse_ReturnsConditionElseBranch()
    {
        // Arrange
        var elseAction1 = new Management.Actions.Action("ElseAction1", ActionType.Action);
        var elseAction2 = new Management.Actions.Action("ElseAction2", ActionType.Action);

        var condition = new ConditionAction("Check", ActionType.Condition);
        condition.ElseActions.Add(elseAction1);
        condition.ElseActions.Add(elseAction2);

        IList<BaseAction> actions = [condition];
        const string path = "Check.else";

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void NavigateToPath_WithConditionWithoutBranch_ReturnsAllBranchActions()
    {
        // Arrange
        var trueAction = new Management.Actions.Action("TrueAction", ActionType.Action);
        var falseAction = new Management.Actions.Action("FalseAction", ActionType.Action);

        var condition = new ConditionAction("MyCondition", ActionType.Condition);
        condition.DefaultActions.Add(trueAction);
        condition.ElseActions.Add(falseAction);

        IList<BaseAction> actions = [condition];
        const string path = "MyCondition"; // No branch specified

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert - Should return all actions from both branches
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Contains.Item(trueAction));
        Assert.That(result, Contains.Item(falseAction));
    }

    [Test]
    public void NavigateToPath_WithSwitchWithoutCase_ReturnsAllCaseActions()
    {
        // Arrange
        var case1Action = new Management.Actions.Action("Case1Action", ActionType.Action);
        var case2Action = new Management.Actions.Action("Case2Action", ActionType.Action);

        var case1 = new SwitchCase { Name = "Case1" };
        case1.Actions.Add(case1Action);

        var case2 = new SwitchCase { Name = "Case2" };
        case2.Actions.Add(case2Action);

        var switchAction = new SwitchAction("MySwitch", ActionType.Switch);
        switchAction.Cases.Add(case1);
        switchAction.Cases.Add(case2);

        IList<BaseAction> actions = [switchAction];
        const string path = "MySwitch"; // No case specified

        // Act
        var result = ActionPathNavigator.NavigateToPath(actions, path);

        // Assert - Should return all actions from all cases
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Contains.Item(case1Action));
        Assert.That(result, Contains.Item(case2Action));
    }
}