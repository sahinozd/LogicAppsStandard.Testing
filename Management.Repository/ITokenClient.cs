using LogicApps.Management.Models.General;

namespace LogicApps.Management.Repository;

/// <summary>
/// Interface for retrieving OAuth tokens for Azure authentication.
/// </summary>
public interface ITokenClient
{
    /// <summary>
    /// Retrieves an OAuth access token using client credentials flow.
    /// </summary>
    /// <param name="clientId">The client ID (Application ID).</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <param name="scope">The scope for the token.</param>
    /// <param name="tenantId">The Azure AD tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The OAuth token containing the access token and metadata.</returns>
    Task<OAuthToken> GetTokenAsync(string? clientId, string? clientSecret, string? scope, string? tenantId, CancellationToken cancellationToken);
}