using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace LogicApps.Management.Repository;

/// <summary>
/// HTTP client implementation for Azure Management API with built-in authorization.
/// This client handles authentication and basic HTTP operations without retry logic.
/// Retry logic is handled at the repository layer.
/// </summary>
public sealed class AzureHttpClient : IAzureHttpClient
{
    private readonly string _scope, _tenantId, _clientId, _clientSecret;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenClient _tokenClient;

    // Acquire this before reading or writing _accessToken / _tokenExpiresAt.
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    // Refresh the token 60 seconds before it actually expires so in-flight requests
    // are never sent with a token that is about to expire.
    private static readonly TimeSpan TokenExpiryBuffer = TimeSpan.FromSeconds(60);

    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the AzureHttpClient class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
    /// <param name="tokenClient">The token client for retrieving OAuth tokens.</param>
    /// <param name="baseAddress">The base address for the Azure Management API.</param>
    /// <param name="tenantId">The Entra tenant ID.</param>
    /// <param name="clientId">The Client ID (Application ID of the Service Principal).</param>
    /// <param name="clientSecret">The Client Secret.</param>
    public AzureHttpClient(IHttpClientFactory httpClientFactory, ITokenClient tokenClient, Uri baseAddress, string tenantId, string clientId, string clientSecret)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _tokenClient = tokenClient ?? throw new ArgumentNullException(nameof(tokenClient));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        
        ArgumentNullException.ThrowIfNull(baseAddress);
        _scope = $"{baseAddress}.default";
    }

    /// <summary>
    /// Sends an HTTP GET request and deserializes the response to the specified type.
    /// </summary>
    public async Task<T?> GetAsync<T>(Uri requestUri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        await AuthorizeAsync(cancellationToken).ConfigureAwait(false);

        var httpClient = _httpClientFactory.CreateClient("AzureManagementClient");
        ConfigureAuthorization(httpClient);

        var response = await ExecuteAuthorizedAsync(httpClient, () => httpClient.GetAsync(requestUri, cancellationToken), cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        var resultContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(resultContent);
    }

    /// <summary>
    /// Sends an HTTP GET request and returns the raw HTTP response.
    /// </summary>
    public async Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        await AuthorizeAsync(cancellationToken).ConfigureAwait(false);

        var httpClient = _httpClientFactory.CreateClient("AzureManagementClient");
        ConfigureAuthorization(httpClient);

        return await ExecuteAuthorizedAsync(httpClient, () => httpClient.GetAsync(requestUri, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP POST request with optional content and deserializes the response to the specified type.
    /// </summary>
    public async Task<T?> PostAsync<T>(Uri requestUri, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        await AuthorizeAsync(cancellationToken).ConfigureAwait(false);

        var httpClient = _httpClientFactory.CreateClient("AzureManagementClient");
        ConfigureAuthorization(httpClient);

        var response = await ExecuteAuthorizedAsync(httpClient, () => httpClient.PostAsync(requestUri, content, cancellationToken), cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        var resultContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(resultContent);
    }

    /// <summary>
    /// Sends an HTTP POST request with optional content and custom headers.
    /// </summary>
    public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent? content, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        await AuthorizeAsync(cancellationToken).ConfigureAwait(false);

        var httpClient = _httpClientFactory.CreateClient("AzureManagementClient");
        ConfigureAuthorization(httpClient);

        if (headers != null)
        {
            foreach (var header in headers)
            {
                if (httpClient.DefaultRequestHeaders.Contains(header.Key))
                {
                    httpClient.DefaultRequestHeaders.Remove(header.Key);
                }

                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        var response = await ExecuteAuthorizedAsync(httpClient, () => httpClient.PostAsync(requestUri, content, cancellationToken), cancellationToken).ConfigureAwait(false);

        return response;
    }

    /// <summary>
    /// Sends an HTTP PUT request with optional content.
    /// </summary>
    public async Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        await AuthorizeAsync(cancellationToken).ConfigureAwait(false);

        var httpClient = _httpClientFactory.CreateClient("AzureManagementClient");
        ConfigureAuthorization(httpClient);

        return await ExecuteAuthorizedAsync(httpClient, () => httpClient.PutAsync(requestUri, content, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a public (non-authenticated) HTTP GET request and deserializes the response to the specified type.
    /// Used for public endpoints like Logic App trigger URLs, webhooks, etc.
    /// </summary>
    public async Task<T?> GetPublicAsync<T>(Uri requestUri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        var httpClient = _httpClientFactory.CreateClient("AzurePublicHttpClient");
        var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        var resultContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(resultContent);
    }

    /// <summary>
    /// Sends a public (non-authenticated) HTTP GET request and returns the raw HTTP response.
    /// Used for public endpoints like Logic App trigger URLs, webhooks, etc.
    /// </summary>
    public async Task<HttpResponseMessage> GetPublicAsync(Uri requestUri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        var httpClient = _httpClientFactory.CreateClient("AzurePublicHttpClient");
        return await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a public (non-authenticated) HTTP POST request with optional content and custom headers.
    /// Used for public endpoints like Logic App trigger URLs, webhooks, etc.
    /// </summary>
    public async Task<HttpResponseMessage> PostPublicAsync(Uri requestUri, HttpContent? content, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        var httpClient = _httpClientFactory.CreateClient("AzurePublicHttpClient");

        if (headers != null)
        {
            foreach (var header in headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        return await httpClient.PostAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures a valid access token is available.
    /// Acquires a new token when none exists, when the current token has expired
    /// (or is within the expiry buffer), or when the server returns 401.
    /// Thread-safe: at most one token request is in-flight at a time.
    /// </summary>
    private async Task AuthorizeAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: no lock needed when the token is still fresh.
        if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
        {
            return;
        }

        await _authLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Recheck inside the lock: another thread may have refreshed already.
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
            {
                return;
            }

            var token = await _tokenClient.GetTokenAsync(_clientId, _clientSecret, _scope, _tenantId, cancellationToken).ConfigureAwait(false);
            _accessToken = token.AccessToken;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn) - TokenExpiryBuffer;
        }
        finally
        {
            _authLock.Release();
        }
    }

    /// <summary>
    /// Executes the HTTP action and, if the server returns 401, refreshes the access token
    /// and retries once with the updated authorization header.
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteAuthorizedAsync(HttpClient httpClient, Func<Task<HttpResponseMessage>> action, CancellationToken cancellationToken)
    {
        var response = await action().ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        return await RetryWithFreshTokenAsync(() =>
        {
            ConfigureAuthorization(httpClient);
            return action();
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Forces a token refresh and retries the HTTP action once. Called when the server returns 401.
    /// </summary>
    private async Task<HttpResponseMessage> RetryWithFreshTokenAsync(Func<Task<HttpResponseMessage>> action, CancellationToken cancellationToken)
    {
        // Invalidate the cached token so AuthorizeAsync will fetch a new one.
        await _authLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _accessToken = null;
            _tokenExpiresAt = DateTimeOffset.MinValue;
        }
        finally
        {
            _authLock.Release();
        }

        await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
        return await action().ConfigureAwait(false);
    }

    /// <summary>
    /// Configures the authorization header on the HTTP client.
    /// </summary>
    private void ConfigureAuthorization(HttpClient httpClient)
    {
        if (!string.IsNullOrEmpty(_accessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
    public void Dispose()
    {
        if (_disposedValue)
        {
            return;
        }

        _authLock.Dispose();
        _disposedValue = true;
    }
}