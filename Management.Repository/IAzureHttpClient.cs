namespace LogicApps.Management.Repository;

/// <summary>
/// Abstraction for Azure Management API HTTP operations with built-in authorization and retry logic.
/// </summary>
public interface IAzureHttpClient : IDisposable
{
    /// <summary>
    /// Sends an HTTP GET request and deserializes the response to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="requestUri">The absolute URI for the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The deserialized response object, or null if the response is empty.</returns>
    Task<T?> GetAsync<T>(Uri requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an HTTP GET request and returns the raw HTTP response.
    /// </summary>
    /// <param name="requestUri">The absolute URI for the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an HTTP POST request with optional content and deserializes the response to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="requestUri">The absolute URI for the request.</param>
    /// <param name="content">The HTTP content to send. Can be null.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The deserialized response object, or null if the response is empty.</returns>
    Task<T?> PostAsync<T>(Uri requestUri, HttpContent? content = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an HTTP POST request with optional content and custom headers.
    /// </summary>
    /// <param name="requestUri">The absolute URI for the request.</param>
    /// <param name="content">The HTTP content to send. Can be null.</param>
    /// <param name="headers">Optional dictionary of custom headers to include in the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent? content, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an HTTP PUT request with optional content.
    /// </summary>
    /// <param name="requestUri">The absolute URI for the request.</param>
    /// <param name="content">The HTTP content to send. Can be null.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent? content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a public (non-authenticated) HTTP GET request and deserializes the response to the specified type.
    /// Used for public endpoints like Logic App trigger URLs, webhooks, etc.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="requestUri">The absolute URI for the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The deserialized response object, or null if the response is empty.</returns>
    Task<T?> GetPublicAsync<T>(Uri requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a public (non-authenticated) HTTP GET request and returns the raw HTTP response.
    /// Used for public endpoints like Logic App trigger URLs, webhooks, etc.
    /// </summary>
    /// <param name="requestUri">The absolute URI for the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> GetPublicAsync(Uri requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a public (non-authenticated) HTTP POST request with optional content and custom headers.
    /// Used for public endpoints like Logic App trigger URLs, webhooks, etc.
    /// </summary>
    /// <param name="requestUri">The absolute URI for the request.</param>
    /// <param name="content">The HTTP content to send. Can be null.</param>
    /// <param name="headers">Optional dictionary of custom headers to include in the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> PostPublicAsync(Uri requestUri, HttpContent? content, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
}