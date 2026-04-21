using LogicApps.Management.Actions;
using LogicApps.Management.Models.Enums;

namespace LogicApps.Management.Tests.Actions;

/// <summary>
/// Mock implementation of BaseAction for testing purposes
/// </summary>
internal sealed class MockAction(string name, ActionType actionType) : BaseAction(name, actionType);