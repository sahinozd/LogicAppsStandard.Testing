using LogicApps.Management.Models.General;
using Newtonsoft.Json;

namespace LogicApps.Management.Repository;

/// <summary>
/// Client for retrieving OAuth tokens from Microsoft Entra ID (formerly Azure AD).
/// </summary>
public sealed class EntraTokenClient : ITokenClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntraTokenClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public EntraTokenClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <summary>
    /// Retrieves an OAuth access token using client credentials flow.
    /// </summary>
    public async Task<OAuthToken> GetTokenAsync(string? clientId, string? clientSecret, string? scope, string? tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        ArgumentException.ThrowIfNullOrEmpty(clientSecret);
        ArgumentException.ThrowIfNullOrEmpty(scope);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var uri = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token");
        var httpClient = _httpClientFactory.CreateClient("EntraTokenClient");

        var form = new Dictionary<string, string>
        {
            {"grant_type", "client_credentials"},
            {"client_id", clientId},
            {"client_secret", clientSecret},
            {"scope", scope}
        };

        using var content = new FormUrlEncodedContent(form);
        var result = await httpClient.PostAsync(uri, content, cancellationToken).ConfigureAwait(false);
        result.EnsureSuccessStatusCode();

        var jsonContent = await result.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return JsonConvert.DeserializeObject<OAuthToken>(jsonContent)!;
    }
}