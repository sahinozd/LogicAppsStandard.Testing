using Newtonsoft.Json;

namespace LogicApps.Management.Models.General;

/// <summary>
/// Represents an OAuth 2.0 token response.
/// </summary>
public sealed class OAuthToken
{
    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token type (typically "Bearer").
    /// </summary>
    [JsonProperty("token_type")]
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of seconds until the token expires.
    /// </summary>
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the extended expiration time in seconds.
    /// </summary>
    [JsonProperty("ext_expires_in")]
    public int ExtExpiresIn { get; set; }
}
