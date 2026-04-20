namespace LogicApps.TestFramework.Specifications.Tests.Helpers.Test_classes;

public sealed record NestedClass
{
    public string? NestedStringProperty { get; set; }

    public int NestedIntProperty { get; set; }

    public DeepNestedClass? DeepNested { get; set; }
}