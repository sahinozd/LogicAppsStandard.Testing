using System.Net;
using NSubstitute;
using NUnit.Framework;

namespace LogicApps.Management.Repository.Tests;

[TestFixture]
internal sealed class EntraTokenClientTests
{
    private IHttpClientFactory _httpClientFactory;
    private HttpClient _httpClient;
    private HttpMessageHandler _httpMessageHandler;

    [SetUp]
    public void SetUp()
    {
        const string tokenResponse = """{"token_type": "Bearer","expires_in": 3599,"ext_expires_in": 3599,"access_token": "eyMockToken123"}""";
        _httpMessageHandler = new MockHttpMessageHandler(tokenResponse, HttpStatusCode.OK);
        _httpClient = new HttpClient(_httpMessageHandler)
        {
            BaseAddress = new Uri("https://login.microsoftonline.com/")
        };

        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpMessageHandler.Dispose();
        _httpClient.Dispose();
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_HttpClientFactory_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new EntraTokenClient(null!);
        });

        Assert.That(exception.ParamName, Is.EqualTo("httpClientFactory"));
    }

    [Test]
    public async Task GetToken_Should_Return_Token()
    {
        // Arrange
        var entraTokenClient = new EntraTokenClient(_httpClientFactory);

        // Act
        var token = await entraTokenClient.GetTokenAsync("clientId", "clientSecret", "scope", "tenant", CancellationToken.None).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(token, Is.Not.Null);
            Assert.That(token.AccessToken, Is.EqualTo("eyMockToken123"));
            Assert.That(token.TokenType, Is.EqualTo("Bearer"));
            Assert.That(token.ExpiresIn, Is.EqualTo(3599));
        }
    }

    [Test]
    public void GetToken_Should_Throw_When_ClientId_Is_Null()
    {
        // Arrange
        var entraTokenClient = new EntraTokenClient(_httpClientFactory);

        // Act & Assert - ArgumentNullException is a subclass of ArgumentException
        Assert.ThrowsAsync<ArgumentNullException>(() => entraTokenClient.GetTokenAsync(null!, "secret", "scope", "tenant", CancellationToken.None));
    }

    [Test]
    public void GetToken_Should_Throw_When_ClientSecret_Is_Null()
    {
        // Arrange
        var entraTokenClient = new EntraTokenClient(_httpClientFactory);

        // Act & Assert - ArgumentNullException is a subclass of ArgumentException
        Assert.ThrowsAsync<ArgumentNullException>(() => entraTokenClient.GetTokenAsync("clientId", null!, "scope", "tenant", CancellationToken.None));
    }

    [Test]
    public void GetToken_Should_Throw_When_Scope_Is_Null()
    {
        // Arrange
        var entraTokenClient = new EntraTokenClient(_httpClientFactory);

        // Act & Assert - ArgumentNullException is a subclass of ArgumentException
        Assert.ThrowsAsync<ArgumentNullException>(() => entraTokenClient.GetTokenAsync("clientId", "secret", null!, "tenant", CancellationToken.None));
    }

    [Test]
    public void GetToken_Should_Throw_When_TenantId_Is_Null()
    {
        // Arrange
        var entraTokenClient = new EntraTokenClient(_httpClientFactory);

        // Act & Assert - ArgumentNullException is a subclass of ArgumentException
        Assert.ThrowsAsync<ArgumentNullException>(() => entraTokenClient.GetTokenAsync("clientId", "secret", "scope", null!, CancellationToken.None));
    }

    [Test]
    public async Task GetToken_Should_Use_Correct_Token_Endpoint()
    {
        // Arrange
        var entraTokenClient = new EntraTokenClient(_httpClientFactory);
        const string tenantId = "tenant-123";

        // Act
        await entraTokenClient.GetTokenAsync("clientId", "secret", "scope", tenantId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        var mockHandler = (MockHttpMessageHandler)_httpMessageHandler;
        var request = mockHandler.Requests[0];
        Assert.That(request.RequestUri?.AbsoluteUri, Does.Contain($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"));
    }
}
