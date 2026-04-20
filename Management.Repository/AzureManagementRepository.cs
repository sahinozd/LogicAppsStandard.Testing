namespace LogicApps.Management.Repository;

/// <summary>
/// Repository for accessing the Azure Management API with built-in retry logic for throttling.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AzureManagementRepository"/> class.
/// </remarks>
/// <param name="httpClient">The Azure HTTP client with built-in authorization.</param>
/// <param name="baseAddress">The base URI for the Azure Management API.</param>
public sealed class AzureManagementRepository(IAzureHttpClient httpClient, Uri baseAddress) : IAzureManagementRepository, IDisposable
{
    private readonly IAzureHttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly Uri _baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
    private bool _disposed;

    /// <summary>
    /// Sends an HTTP GET or POST request to the specified relative URI and deserializes the response body as an object of type T.
    /// </summary>
    /// <typeparam name="T">The type into which the response content is deserialized.</typeparam>
    /// <param name="relativeUri">The relative URI of the resource to request.</param>
    /// <param name="content">The HTTP content to send with the request. If null, a GET request is sent; otherwise, a POST request is sent.</param>
    /// <param name="retryCount">Not used - kept for backwards compatibility.</param>
    /// <returns>The deserialized object of type T, or null if the response content is empty.</returns>
    public async Task<T?> GetObjectAsync<T>(Uri relativeUri, HttpContent? content = null, int retryCount = 3)
    {
        var requestUri = new Uri(_baseAddress, relativeUri);
        
        return content == null
            ? await WithRetryAsync(() => _httpClient.GetAsync<T>(requestUri), requestUri.ToString()).ConfigureAwait(false)
            : await WithRetryAsync(() => _httpClient.PostAsync<T>(requestUri, content), requestUri.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP GET request to a public endpoint and deserializes the response content to an object of type T.
    /// </summary>
    /// <typeparam name="T">The type to which the response content is deserialized.</typeparam>
    /// <param name="requestUri">The URI of the public resource to retrieve.</param>
    /// <returns>A deserialized object of type T if the response content can be converted; otherwise, null.</returns>
    public async Task<T?> GetObjectPublicAsync<T>(Uri requestUri)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        return await WithRetryAsync(() => _httpClient.GetPublicAsync<T>(requestUri), requestUri.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a GET request to the specified URI as an asynchronous operation.
    /// </summary>
    /// <param name="requestUri">The URI to which the GET request is sent.</param>
    /// <returns>The HTTP response message received from the server.</returns>
    public async Task<HttpResponseMessage> GetAsync(Uri? requestUri)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        return await WithRetryAsync(() => _httpClient.GetAsync(requestUri), requestUri.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP GET request to a public endpoint and returns the response message.
    /// </summary>
    /// <param name="requestUri">The URI to which the GET request is sent.</param>
    /// <returns>The HTTP response message returned by the server.</returns>
    public async Task<HttpResponseMessage> GetPublicAsync(Uri requestUri)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        return await WithRetryAsync(() => _httpClient.GetPublicAsync(requestUri), requestUri.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a POST request to the specified URI with the provided HTTP content as an asynchronous operation.
    /// </summary>
    /// <param name="requestUri">The URI to which the POST request is sent.</param>
    /// <param name="content">The HTTP content to send with the request.</param>
    /// <returns>The HTTP response message returned by the server.</returns>
    public async Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        return await WithRetryAsync(() => _httpClient.PostAsync(requestUri, content), requestUri.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a POST request to a public endpoint with optional content and headers.
    /// </summary>
    /// <param name="requestUri">The URI to which the POST request is sent.</param>
    /// <param name="content">The HTTP request content to send.</param>
    /// <param name="headers">An optional collection of request headers to include in the POST request.</param>
    /// <returns>The HTTP response message returned by the server.</returns>
    public async Task<HttpResponseMessage> PostPublicAsync(Uri requestUri, HttpContent? content, Dictionary<string, string>? headers = null)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        return await WithRetryAsync(() => _httpClient.PostPublicAsync(requestUri, content, headers), requestUri.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a POST request to the specified URI with the provided content and custom headers, and returns the HTTP response.
    /// </summary>
    /// <param name="requestUri">The URI to which the POST request is sent.</param>
    /// <param name="content">The HTTP request content to send.</param>
    /// <param name="headers">A dictionary containing header names and values to include in the request.</param>
    /// <returns>The HTTP response message returned by the server.</returns>
    public async Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content, Dictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        ArgumentNullException.ThrowIfNull(headers);
        return await WithRetryAsync(() => _httpClient.PostAsync(requestUri, content, headers), requestUri.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP PUT request to the specified URI with the provided content as an asynchronous operation.
    /// </summary>
    /// <param name="requestUri">The URI to which the PUT request is sent.</param>
    /// <param name="content">The HTTP request content to send to the server.</param>
    /// <returns>The HTTP response message returned by the server.</returns>
    public async Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent? content)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        return await WithRetryAsync(() => _httpClient.PutAsync(requestUri, content), requestUri.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the specified HTTP action with automatic retries for throttling (HTTP 429) responses.
    /// </summary>
    /// <remarks>If the HTTP response status code is 429 (Too Many Requests), the method waits for a calculated delay before retrying the request.
    /// For other status codes, the response is returned immediately without further retries.</remarks>
    /// <param name="action">A delegate that performs the HTTP request and returns a task representing the asynchronous operation.</param>
    /// <param name="requestUri">The URI of the HTTP request, used for logging and exception messages.</param>
    /// <param name="maxRetries">The maximum number of retry attempts to perform if the request is throttled. The default is 5.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message returned by the action.</returns>
    /// <exception cref="HttpRequestException">Thrown if the maximum number of retries is exceeded due to repeated throttling responses.</exception>
    private static async Task<HttpResponseMessage> WithRetryAsync(Func<Task<HttpResponseMessage>> action, string requestUri, int maxRetries = 5)
    {
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var response = await action().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if ((int)response.StatusCode != 429)
            {
                Console.WriteLine($"No throttling happened on URI {requestUri}. Total of tries: {attempt + 1}/{maxRetries}.");
                return response;
            }

            var delay = GetRetryDelay(response, attempt);
            Console.WriteLine($"Throttled (429) on URI {requestUri}. Waiting {delay.TotalSeconds} seconds before retry {attempt + 1}/{maxRetries}.");

            await Task.Delay(delay).ConfigureAwait(false);
        }

        throw new HttpRequestException($"Could not get response from {requestUri}. Exceeded {maxRetries} retries due to throttling.");
    }

    /// <summary>
    /// Executes the specified action with automatic retries for throttling, returning a typed result.
    /// </summary>
    private static async Task<T?> WithRetryAsync<T>(Func<Task<T?>> action, string requestUri, int maxRetries = 5)
    {
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (HttpRequestException) when (attempt < maxRetries - 1)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                Console.WriteLine($"Request failed for URI {requestUri}. Waiting {delay.TotalSeconds} seconds before retry {attempt + 1}/{maxRetries}.");
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        throw new HttpRequestException($"Could not get response from {requestUri}. Exceeded {maxRetries} retries.");
    }

    /// <summary>
    /// Calculates the delay before the next retry attempt based on the HTTP response and the current attempt number.
    /// </summary>
    /// <remarks>If the 'Retry-After' header is present in the response, its value is interpreted as either a number of seconds or an absolute date and time.
    /// If the header is not present or cannot be parsed, the method falls back to an exponential backoff strategy based on the attempt number.</remarks>
    /// <param name="response">The HTTP response message to inspect for a 'Retry-After' header, which may specify the recommended delay before retrying.</param>
    /// <param name="attempt">The zero-based index of the current retry attempt. Used to calculate the delay if the response does not specify one.</param>
    /// <returns>A TimeSpan representing the amount of time to wait before the next retry. If the response includes a valid 'Retry-After' header, its value is used; otherwise, an exponential backoff delay is returned.</returns>
    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (!response.Headers.TryGetValues("Retry-After", out var values))
        {
            return TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
        }

        var retryAfter = string.Join(",", values);

        if (int.TryParse(retryAfter, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        if (!DateTimeOffset.TryParse(retryAfter, out var retryDate))
        {
            return TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
        }

        var delay = retryDate - DateTimeOffset.UtcNow;
        return delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Releases all resources used by the repository.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _httpClient.Dispose();
        _disposed = true;
    }
}