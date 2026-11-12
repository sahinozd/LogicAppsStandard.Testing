using System.Net;
using System.Text;
using Azure.Core;
using LogicApps.Management.Models.General;
using NSubstitute;
using NUnit.Framework;

namespace LogicApps.Management.Repository.Tests;

/// <summary>
/// Tests for <see cref="AzureHttpClient"/> when it authenticates with an Azure.Identity <see cref="TokenCredential"/>.
/// </summary>
[TestFixture]
internal sealed class AzureHttpClientTokenCredentialTests
{
    private const string TestBaseAddress = "https://management.azure.com/";
    private static readonly Uri RequestUri = new("https://management.azure.com/subscriptions");

    private SequenceHttpMessageHandler _handler;
    private HttpClient _httpClient;
    private IHttpClientFactory _httpClientFactory;
    private TokenCredential _credential;

    [SetUp]
    public void SetUp()
    {
        _handler = new SequenceHttpMessageHandler();
        _httpClient = new HttpClient(_handler);
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_httpClient);
        _credential = Substitute.For<TokenCredential>();
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_HttpClientFactory_Is_Null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => _ = new AzureHttpClient(null!, _credential, new Uri(TestBaseAddress)));

        Assert.That(exception.ParamName, Is.EqualTo("httpClientFactory"));
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_Credential_Is_Null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => _ = new AzureHttpClient(_httpClientFactory, null!, new Uri(TestBaseAddress)));

        Assert.That(exception.ParamName, Is.EqualTo("credential"));
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_BaseAddress_Is_Null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => _ = new AzureHttpClient(_httpClientFactory, _credential, null!));

        Assert.That(exception.ParamName, Is.EqualTo("baseAddress"));
    }

    [Test]
    public async Task GetAsync_Should_Request_Token_For_Default_Scope_Of_BaseAddress_And_Send_Bearer_Header()
    {
        // Arrange
        SetupTokens(("token-1", DateTimeOffset.UtcNow.AddHours(1)));
        using var client = new AzureHttpClient(_httpClientFactory, _credential, new Uri("https://mystorage.blob.core.windows.net"));

        // Act
        using var response = await client.GetAsync(RequestUri).ConfigureAwait(false);

        // Assert
        await _credential.Received(1).GetTokenAsync(
            Arg.Is<TokenRequestContext>(context => context.Scopes.Length == 1 && context.Scopes[0] == "https://mystorage.blob.core.windows.net/.default"),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
        Assert.That(_handler.AuthorizationHeaders, Is.EqualTo(Headers("Bearer token-1")));
    }

    [Test]
    public async Task Requests_Should_Reuse_Cached_Token_While_It_Is_Valid()
    {
        // Arrange
        SetupTokens(("token-1", DateTimeOffset.UtcNow.AddHours(1)));
        using var client = new AzureHttpClient(_httpClientFactory, _credential, new Uri(TestBaseAddress));

        // Act
        using (await client.GetAsync(RequestUri).ConfigureAwait(false)) { }
        using (await client.PutAsync(RequestUri, null).ConfigureAwait(false)) { }

        // Assert
        await _credential.Received(1).GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        Assert.That(_handler.AuthorizationHeaders, Is.EqualTo(Headers("Bearer token-1", "Bearer token-1")));
    }

    [Test]
    public async Task Requests_Should_Acquire_New_Token_When_Token_Expires_Within_Buffer()
    {
        // Arrange: the first token expires in 30 seconds, inside the 60-second refresh buffer.
        SetupTokens(("token-1", DateTimeOffset.UtcNow.AddSeconds(30)), ("token-2", DateTimeOffset.UtcNow.AddHours(1)));
        using var client = new AzureHttpClient(_httpClientFactory, _credential, new Uri(TestBaseAddress));

        // Act
        using (await client.GetAsync(RequestUri).ConfigureAwait(false)) { }
        using (await client.GetAsync(RequestUri).ConfigureAwait(false)) { }

        // Assert
        await _credential.Received(2).GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        Assert.That(_handler.AuthorizationHeaders, Is.EqualTo(Headers("Bearer token-1", "Bearer token-2")));
    }

    [Test]
    public async Task Request_Should_Refresh_Token_And_Retry_Once_When_Server_Returns_Unauthorized()
    {
        // Arrange
        SetupTokens(("token-1", DateTimeOffset.UtcNow.AddHours(1)), ("token-2", DateTimeOffset.UtcNow.AddHours(1)));
        _handler.StatusCodes.Enqueue(HttpStatusCode.Unauthorized);
        _handler.StatusCodes.Enqueue(HttpStatusCode.OK);
        using var client = new AzureHttpClient(_httpClientFactory, _credential, new Uri(TestBaseAddress));

        // Act
        using var response = await client.GetAsync(RequestUri).ConfigureAwait(false);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        await _credential.Received(2).GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        Assert.That(_handler.AuthorizationHeaders, Is.EqualTo(Headers("Bearer token-1", "Bearer token-2")));
    }

    [Test]
    public async Task ClientSecret_Constructor_Should_Cache_Token_For_ExpiresIn_Seconds()
    {
        // Arrange: the ITokenClient path is adapted to a TokenCredential internally.
        var tokenClient = Substitute.For<ITokenClient>();
        tokenClient
            .GetTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new OAuthToken { AccessToken = "secret-token", TokenType = "Bearer", ExpiresIn = 3600 }));
        using var client = new AzureHttpClient(_httpClientFactory, tokenClient, new Uri(TestBaseAddress), "tenant", "client", "secret");

        // Act
        using (await client.GetAsync(RequestUri).ConfigureAwait(false)) { }
        using (await client.GetAsync(RequestUri).ConfigureAwait(false)) { }

        // Assert
        await tokenClient.Received(1).GetTokenAsync("client", "secret", $"{TestBaseAddress}.default", "tenant", Arg.Any<CancellationToken>()).ConfigureAwait(false);
        Assert.That(_handler.AuthorizationHeaders, Is.EqualTo(Headers("Bearer secret-token", "Bearer secret-token")));
    }

    private void SetupTokens(params (string Token, DateTimeOffset ExpiresOn)[] tokens)
    {
        var results = tokens.Select(token => new ValueTask<AccessToken>(new AccessToken(token.Token, token.ExpiresOn))).ToArray();
        _credential
            .GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(results[0], results.Skip(1).ToArray());
    }

    private static string[] Headers(params string[] values) => values;

    /// <summary>
    /// Returns the queued status codes in order (200 OK once the queue is empty) and records each request's Authorization header.
    /// </summary>
    private sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        public Queue<HttpStatusCode> StatusCodes { get; } = new();

        public List<string?> AuthorizationHeaders { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationHeaders.Add(request.Headers.Authorization?.ToString());
            var statusCode = StatusCodes.Count > 0 ? StatusCodes.Dequeue() : HttpStatusCode.OK;

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        }
    }
}
