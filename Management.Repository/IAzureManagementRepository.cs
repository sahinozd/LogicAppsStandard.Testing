namespace LogicApps.Management.Repository;

public interface IAzureManagementRepository
{
    /// <summary>
    /// Sends an HTTP GET or POST request to the specified relative URI and deserializes the response body as an object of type T.
    /// </summary>
    /// <remarks>The method automatically authorizes the request before sending it. Retries are performed according to the specified retry count if the request fails.</remarks>
    /// <typeparam name="T">The type into which the response content is deserialized.</typeparam>
    /// <param name="relativeUri">The relative URI of the resource to request. Combined with the base address to form the full request URI.</param>
    /// <param name="content">The HTTP content to send with the request. If null, a GET request is sent; otherwise, a POST request is sent with the specified content.</param>
    /// <param name="retryCount">The maximum number of times to retry the request if it fails. Must be greater than or equal to 1.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized object of type T, or null if the response content is empty.</returns>
    Task<T?> GetObjectAsync<T>(Uri relativeUri, HttpContent? content = null, int retryCount = 3);

    /// <summary>
    /// Sends an HTTP GET request to a public endpoint without authorization and deserializes the response content to an object of type T.
    /// </summary>
    /// <remarks>This method uses a public HTTP client and does not include authentication headers in the request.
    /// Used for public endpoints like Logic App trigger URLs, webhooks, etc. The response content is expected to be in a format compatible with the specified type T.</remarks>
    /// <typeparam name="T">The type to which the response content is deserialized.</typeparam>
    /// <param name="requestUri">The URI of the public resource to retrieve. Cannot be null.</param>
    /// <returns>A deserialized object of type T if the response content can be converted; otherwise, null.</returns>
    Task<T?> GetObjectPublicAsync<T>(Uri requestUri);

    /// <summary>
    /// Sends a GET request to the specified URI as an asynchronous operation.
    /// </summary>
    /// <remarks>This method automatically authorizes the request before sending and applies retry logic for transient failures. The caller is responsible for disposing the returned HttpResponseMessage.</remarks>
    /// <param name="requestUri">The URI to which the GET request is sent. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message received from the server.</returns>
    Task<HttpResponseMessage> GetAsync(Uri? requestUri);

    /// <summary>
    /// Sends an HTTP GET request to a public endpoint and returns the response message.
    /// </summary>
    /// <remarks>This method sends a request to public endpoints without authentication. Used for Logic App trigger URLs, webhooks, etc. The request is executed with a retry policy as defined by the implementation of WithRetryAsync.</remarks>
    /// <param name="requestUri">The URI to which the GET request is sent. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message returned by the server.</returns>
    Task<HttpResponseMessage> GetPublicAsync(Uri requestUri);

    /// <summary>
    /// Sends a POST request to the specified URI with the provided HTTP content as an asynchronous operation.
    /// </summary>
    /// <remarks>This method automatically handles authorization and retries the request according to the configured retry policy. The caller is responsible for disposing the returned HttpResponseMessage.</remarks>
    /// <param name="requestUri">The URI to which the POST request is sent. Cannot be null.</param>
    /// <param name="content">The HTTP content to send with the request. May be null if the request does not require a body.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message returned by the server.</returns>
    Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content);

    /// <summary>
    /// Sends a POST request to a public endpoint with optional content and headers.
    /// </summary>
    /// <remarks>This method sends a request to public endpoints without authentication. Used for Logic App trigger URLs, webhooks, etc.
    /// The operation is retried according to the retry policy defined by WithRetryAsync.</remarks>
    /// <param name="requestUri">The URI to which the POST request is sent. Cannot be null.</param>
    /// <param name="content">The HTTP request content to send. May be null if no content is required.</param>
    /// <param name="headers">An optional collection of request headers to include in the POST request. If null, no additional headers are added.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message returned by the server.</returns>
    Task<HttpResponseMessage> PostPublicAsync(Uri requestUri, HttpContent? content, Dictionary<string, string>? headers);

    /// <summary>
    /// Sends a POST request to the specified URI with the provided content and custom headers, and returns the HTTP response.
    /// </summary>
    /// <remarks>The method applies authorization before sending the request and retries the operation according to the configured retry policy.
    /// Existing headers with the same name are replaced. Callers are responsible for disposing the returned HttpResponseMessage.</remarks>
    /// <param name="requestUri">The URI to which the POST request is sent. Cannot be null.</param>
    /// <param name="content">The HTTP request content to send. May be null if no content is required.</param>
    /// <param name="headers">A dictionary containing header names and values to include in the request. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message returned by the server.</returns>
    Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content, Dictionary<string, string> headers);

    /// <summary>
    /// Sends an HTTP PUT request to the specified URI with the provided content as an asynchronous operation.
    /// </summary>
    /// <remarks>This method ensures authorization before sending the request and automatically retries the operation according to the configured retry policy.
    /// The caller is responsible for disposing the returned HttpResponseMessage.</remarks>
    /// <param name="requestUri">The URI to which the PUT request is sent. Cannot be null.</param>
    /// <param name="content">The HTTP request content to send to the server. May be null if no content is required.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message returned by the server.</returns>
    Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent? content);

    /// <summary>
    /// Releases all resources used by the repository implementation.
    /// </summary>
    void Dispose();
}