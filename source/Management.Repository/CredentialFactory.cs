using Azure.Core;
using Azure.Identity;
using LogicApps.Management.Models.Constants;
using LogicApps.Management.Models.Enums;
using Microsoft.Extensions.Configuration;

namespace LogicApps.Management.Repository;

/// <summary>
/// Creates the Azure credential that matches the configured <see cref="AuthenticationMode"/>.
/// </summary>
public static class CredentialFactory
{
    /// <summary>
    /// Reads the <c>AuthenticationMode</c> configuration key. Returns <see cref="AuthenticationMode.ClientSecret"/>
    /// when the key is missing or empty.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The configured authentication mode.</returns>
    /// <exception cref="InvalidOperationException">The value is not a known authentication mode.</exception>
    public static AuthenticationMode GetAuthenticationMode(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var value = configuration[StringConstants.AuthenticationMode];
        if (string.IsNullOrWhiteSpace(value))
        {
            return AuthenticationMode.ClientSecret;
        }

        if (Enum.TryParse<AuthenticationMode>(value.Trim(), ignoreCase: true, out var mode) && Enum.IsDefined(mode))
        {
            return mode;
        }

        throw new InvalidOperationException($"Unknown {StringConstants.AuthenticationMode} '{value}'. Valid values are: {string.Join(", ", Enum.GetNames<AuthenticationMode>())}.");
    }

    /// <summary>
    /// Creates a <see cref="TokenCredential"/> for the configured authentication mode.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>
    /// The credential, or <see langword="null"/> for <see cref="AuthenticationMode.ClientSecret"/>, which uses the
    /// framework's own client credentials flow (<c>EntraTokenClient</c>).
    /// </returns>
    /// <exception cref="InvalidOperationException">The mode is unknown or required configuration values are missing.</exception>
    public static TokenCredential? Create(IConfiguration configuration)
    {
        var mode = GetAuthenticationMode(configuration);

        switch (mode)
        {
            case AuthenticationMode.ClientSecret:
                EnsureConfigured(configuration, mode, StringConstants.TenantId, StringConstants.ClientId, StringConstants.ClientSecret);
                return null;

            case AuthenticationMode.AzurePipelines:
                EnsureConfigured(configuration, mode, StringConstants.TenantId, StringConstants.ClientId, StringConstants.ServiceConnectionId);
                var systemAccessToken = GetSystemAccessToken(configuration);
                return new AzurePipelinesCredential(
                    configuration[StringConstants.TenantId]!,
                    configuration[StringConstants.ClientId]!,
                    configuration[StringConstants.ServiceConnectionId]!,
                    systemAccessToken);

            case AuthenticationMode.AzureCli:
                var tenantId = configuration[StringConstants.TenantId];
                return string.IsNullOrWhiteSpace(tenantId)
                    ? new AzureCliCredential()
                    : new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId });

            case AuthenticationMode.Default:
                var options = new DefaultAzureCredentialOptions();
                if (!string.IsNullOrWhiteSpace(configuration[StringConstants.TenantId]))
                {
                    options.TenantId = configuration[StringConstants.TenantId];
                }

                return new DefaultAzureCredential(options);

            default:
                throw new InvalidOperationException($"Unsupported {StringConstants.AuthenticationMode} '{mode}'.");
        }
    }

    private static string GetSystemAccessToken(IConfiguration configuration)
    {
        var token = configuration[StringConstants.SystemAccessToken];
        if (string.IsNullOrWhiteSpace(token))
        {
            token = Environment.GetEnvironmentVariable(StringConstants.SystemAccessTokenEnvironmentVariable);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                $"{StringConstants.AuthenticationMode} '{AuthenticationMode.AzurePipelines}' needs the Azure DevOps job access token. " +
                $"Map it to the test task with 'env: {StringConstants.SystemAccessTokenEnvironmentVariable}: $(System.AccessToken)'.");
        }

        return token;
    }

    private static void EnsureConfigured(IConfiguration configuration, AuthenticationMode mode, params string[] keys)
    {
        var missing = keys.Where(key => string.IsNullOrWhiteSpace(configuration[key])).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"{StringConstants.AuthenticationMode} '{mode}' requires these configuration values, which are missing or empty: {string.Join(", ", missing)}.");
        }
    }
}
