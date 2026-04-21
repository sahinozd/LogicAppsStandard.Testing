namespace LogicApps.TestFramework.Specifications.Tests.Helpers.Test_classes;

public sealed record TestClass
{
    public string? StringProperty { get; set; }
    public int IntProperty { get; set; }
    public int? NullableIntProperty { get; set; }
    public NestedClass? Nested { get; set; }
}