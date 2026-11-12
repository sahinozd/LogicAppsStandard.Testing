using Azure.Core;

namespace LogicApps.Management.Repository;

/// <summary>
/// Adapts an <see cref="ITokenClient"/> (client ID + client secret flow) to a <see cref="TokenCredential"/>,
/// so <see cref="AzureHttpClient"/> has a single token acquisition path for every authentication method.
/// </summary>
internal sealed class TokenClientCredential : TokenCredential
{
    private readonly ITokenClient _tokenClient;
    private readonly string _tenantId, _clientId, _clientSecret;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenClientCredential"/> class.
    /// </summary>
    /// <param name="tokenClient">The token client that performs the client credentials flow.</param>
    /// <param name="tenantId">The Entra tenant ID.</param>
    /// <param name="clientId">The client ID (Application ID).</param>
    /// <param name="clientSecret">The client secret.</param>
    public TokenClientCredential(ITokenClient tokenClient, string tenantId, string clientId, string clientSecret)
    {
        _tokenClient = tokenClient ?? throw new ArgumentNullException(nameof(tokenClient));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
    }

    /// <inheritdoc />
    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var scope = string.Join(' ', requestContext.Scopes);
        var token = await _tokenClient.GetTokenAsync(_clientId, _clientSecret, scope, _tenantId, cancellationToken).ConfigureAwait(false);
        return new AccessToken(token.AccessToken, DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn));
    }

    /// <inheritdoc />
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();
}
