using Azure.Identity;
using LogicApps.Management.Models.Enums;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace LogicApps.Management.Repository.Tests;

[TestFixture]
internal sealed class CredentialFactoryTests
{
    private const string TenantId = "12345678-1234-1234-1234-123456789012";
    private const string ClientId = "87654321-4321-4321-4321-210987654321";
    private const string ServiceConnectionId = "11111111-2222-3333-4444-555555555555";

    private string? _originalSystemAccessToken;

    [SetUp]
    public void SetUp()
    {
        _originalSystemAccessToken = Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN");
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", null);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", _originalSystemAccessToken);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void GetAuthenticationMode_Should_Default_To_ClientSecret_When_Not_Set(string? value)
    {
        var configuration = BuildConfiguration(("AuthenticationMode", value));

        Assert.That(CredentialFactory.GetAuthenticationMode(configuration), Is.EqualTo(AuthenticationMode.ClientSecret));
    }

    [TestCase("ClientSecret", AuthenticationMode.ClientSecret)]
    [TestCase("azurepipelines", AuthenticationMode.AzurePipelines)]
    [TestCase("AZURECLI", AuthenticationMode.AzureCli)]
    [TestCase(" Default ", AuthenticationMode.Default)]
    public void GetAuthenticationMode_Should_Parse_Case_Insensitive(string value, AuthenticationMode expected)
    {
        var configuration = BuildConfiguration(("AuthenticationMode", value));

        Assert.That(CredentialFactory.GetAuthenticationMode(configuration), Is.EqualTo(expected));
    }

    [TestCase("ManagedIdentityX")]
    [TestCase("42")]
    public void GetAuthenticationMode_Should_Throw_For_Unknown_Value(string value)
    {
        var configuration = BuildConfiguration(("AuthenticationMode", value));

        var exception = Assert.Throws<InvalidOperationException>(() => CredentialFactory.GetAuthenticationMode(configuration));

        Assert.That(exception.Message, Does.Contain(value).And.Contain("AzurePipelines"));
    }

    [Test]
    public void GetAuthenticationMode_Should_Throw_When_Configuration_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => CredentialFactory.GetAuthenticationMode(null!));
    }

    [Test]
    public void Create_Should_Return_Null_For_ClientSecret_When_All_Values_Are_Set()
    {
        var configuration = BuildConfiguration(("TenantId", TenantId), ("ClientId", ClientId), ("ClientSecret", "secret"));

        Assert.That(CredentialFactory.Create(configuration), Is.Null);
    }

    [Test]
    public void Create_Should_Throw_For_ClientSecret_When_Secret_Is_Missing()
    {
        var configuration = BuildConfiguration(("TenantId", TenantId), ("ClientId", ClientId));

        var exception = Assert.Throws<InvalidOperationException>(() => CredentialFactory.Create(configuration));

        Assert.That(exception.Message, Does.Contain("ClientSecret"));
    }

    [Test]
    public void Create_Should_Return_AzurePipelinesCredential_With_Token_From_Environment()
    {
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", "job-access-token");
        var configuration = BuildConfiguration(
            ("AuthenticationMode", "AzurePipelines"), ("TenantId", TenantId), ("ClientId", ClientId), ("ServiceConnectionId", ServiceConnectionId));

        Assert.That(CredentialFactory.Create(configuration), Is.InstanceOf<AzurePipelinesCredential>());
    }

    [Test]
    public void Create_Should_Return_AzurePipelinesCredential_With_Token_From_Configuration()
    {
        var configuration = BuildConfiguration(
            ("AuthenticationMode", "AzurePipelines"), ("TenantId", TenantId), ("ClientId", ClientId),
            ("ServiceConnectionId", ServiceConnectionId), ("SystemAccessToken", "job-access-token"));

        Assert.That(CredentialFactory.Create(configuration), Is.InstanceOf<AzurePipelinesCredential>());
    }

    [Test]
    public void Create_Should_Throw_For_AzurePipelines_When_System_AccessToken_Is_Missing()
    {
        var configuration = BuildConfiguration(
            ("AuthenticationMode", "AzurePipelines"), ("TenantId", TenantId), ("ClientId", ClientId), ("ServiceConnectionId", ServiceConnectionId));

        var exception = Assert.Throws<InvalidOperationException>(() => CredentialFactory.Create(configuration));

        Assert.That(exception.Message, Does.Contain("SYSTEM_ACCESSTOKEN"));
    }

    [Test]
    public void Create_Should_Throw_For_AzurePipelines_Listing_All_Missing_Values()
    {
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", "job-access-token");
        var configuration = BuildConfiguration(("AuthenticationMode", "AzurePipelines"), ("TenantId", TenantId));

        var exception = Assert.Throws<InvalidOperationException>(() => CredentialFactory.Create(configuration));

        Assert.That(exception.Message, Does.Contain("ClientId").And.Contain("ServiceConnectionId").And.Not.Contain("TenantId,"));
    }

    [Test]
    public void Create_Should_Not_Require_ClientSecret_For_AzurePipelines()
    {
        Environment.SetEnvironmentVariable("SYSTEM_ACCESSTOKEN", "job-access-token");
        var configuration = BuildConfiguration(
            ("AuthenticationMode", "AzurePipelines"), ("TenantId", TenantId), ("ClientId", ClientId),
            ("ClientSecret", ""), ("ServiceConnectionId", ServiceConnectionId));

        Assert.DoesNotThrow(() => CredentialFactory.Create(configuration));
    }

    [TestCase(null)]
    [TestCase(TenantId)]
    public void Create_Should_Return_AzureCliCredential_With_Or_Without_TenantId(string? tenantId)
    {
        var configuration = BuildConfiguration(("AuthenticationMode", "AzureCli"), ("TenantId", tenantId));

        Assert.That(CredentialFactory.Create(configuration), Is.InstanceOf<AzureCliCredential>());
    }

    [TestCase(null)]
    [TestCase(TenantId)]
    public void Create_Should_Return_DefaultAzureCredential_With_Or_Without_TenantId(string? tenantId)
    {
        var configuration = BuildConfiguration(("AuthenticationMode", "Default"), ("TenantId", tenantId));

        Assert.That(CredentialFactory.Create(configuration), Is.InstanceOf<DefaultAzureCredential>());
    }

    private static IConfiguration BuildConfiguration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string?>(value.Key, value.Value)))
            .Build();
}
