using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace LogicApps.Management.Repository;

/// <summary>
/// HTTP client implementation for Azure Management API with built-in authorization.
/// This client handles authentication and basic HTTP operations without retry logic.
/// Retry logic is handled at the repository layer.
/// </summary>
public class AzureHttpClient : IAzureHttpClient
{
    private readonly string _scope, _tenantId, _clientId, _clientSecret;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenClient _tokenClient;
    private bool _disposedValue;
    private bool _authorized;
    private string? _accessToken;

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

        await Authorize().ConfigureAwait(false);

        var httpClient = _httpClientFactory.CreateClient("AzureManagementClient");
        ConfigureAuthorization(httpClient);

        var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

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

        await Authorize().ConfigureAwait(false);

        var httpClient = _httpClientFactory.CreateClient("AzureManagementClient");
        ConfigureAuthorization(httpClient);

        return await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP POST request with optional content and deserializes the response to the specified type.
    /// </summary>
    public async Task<T?> PostAsync<T>(Uri requestUri, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        await Authorize().ConfigureAwait(false);

        var httpClient = _httpClientFactory.CreateClient("AzureManagementClient");
        ConfigureAuthorization(httpClient);

        var response = await httpClient.PostAsync(requestUri, content, cancellationToken).ConfigureAwait(false);

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

        await Authorize().ConfigureAwait(false);

        var httpClient = _httpClientFactory.CreateClient("AzureManagementClient");
        ConfigureAuthorization(httpClient);

        if (headers != null)
        {
            foreach (var header in headers)
            {
                if (httpClient.DefaultRequestHeaders.Contains(header.Key))
                    httpClient.DefaultRequestHeaders.Remove(header.Key);

                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        return await httpClient.PostAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP PUT request with optional content.
    /// </summary>
    public async Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent? content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        await Authorize().ConfigureAwait(false);

        var httpClient = _httpClientFactory.CreateClient("AzureManagementClient");
        ConfigureAuthorization(httpClient);

        return await httpClient.PutAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
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
    /// Performs an asynchronous authorization operation using client credentials and sets the authorization header for subsequent HTTP requests.
    /// </summary>
    private async Task Authorize()
    {
        if (_authorized)
        {
            return;
        }

        var token = await _tokenClient.GetTokenAsync(_clientId, _clientSecret, _scope, _tenantId, CancellationToken.None).ConfigureAwait(false);
        _accessToken = token.AccessToken;
        _authorized = true;
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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the object and optionally releases the managed resources.
    /// </summary>
    /// <remarks>
    /// HttpClients created by IHttpClientFactory are managed by the factory and should not be disposed.
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            // HttpClients created by IHttpClientFactory should not be disposed
            // The factory manages their lifecycle
        }

        _disposedValue = true;
    }
}