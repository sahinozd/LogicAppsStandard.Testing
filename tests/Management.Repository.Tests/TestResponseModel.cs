namespace LogicApps.Management.Repository.Tests;

// Test helper class for deserializing response objects
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "This class is used as a type parameter for generic methods in tests and may be instantiated by the deserialization framework.")]
internal sealed class TestResponseModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

internal sealed partial class AzureManagementRepositoryTests;
