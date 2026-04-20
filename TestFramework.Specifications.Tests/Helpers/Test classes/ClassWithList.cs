namespace LogicApps.TestFramework.Specifications.Tests.Helpers.Test_classes;

public sealed record ClassWithList
{
    public IList<ListItem> Items { get; } = [];
}