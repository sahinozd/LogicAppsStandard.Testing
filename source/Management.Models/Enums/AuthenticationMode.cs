namespace LogicApps.Management.Models.Enums;

/// <summary>
/// How the test framework authenticates against Azure. Set through the <c>AuthenticationMode</c> configuration key.
/// </summary>
public enum AuthenticationMode
{
    /// <summary>
    /// Client ID and client secret of an Entra ID app registration (<c>TenantId</c>, <c>ClientId</c>, <c>ClientSecret</c>).
    /// The default when <c>AuthenticationMode</c> is not set.
    /// </summary>
    ClientSecret,

    /// <summary>
    /// The identity behind an Azure DevOps Azure Resource Manager service connection that uses workload identity
    /// federation (<c>TenantId</c>, <c>ClientId</c>, <c>ServiceConnectionId</c> and the <c>SYSTEM_ACCESSTOKEN</c>
    /// environment variable). Works in any pipeline task, such as <c>DotNetCoreCLI@2</c>.
    /// </summary>
    AzurePipelines,

    /// <summary>
    /// The identity that is signed in to the Azure CLI, for example inside an <c>AzureCLI@2</c> task,
    /// after <c>azure/login</c> in GitHub Actions, or a developer's own <c>az login</c>.
    /// </summary>
    AzureCli,

    /// <summary>
    /// <c>DefaultAzureCredential</c>: tries environment variables, workload identity, managed identity,
    /// Visual Studio, Azure CLI and others in turn. Convenient for local development.
    /// </summary>
    Default,
}
