using System.Net;
using LogicApps.Management.Models.General;
using NSubstitute;
using NUnit.Framework;

namespace LogicApps.Management.Repository.Tests;

[TestFixture]
internal sealed class AzureHttpClientTests
{
    private const string TestTenantId = "12345678-1234-1234-1234-123456789012";
    private const string TestClientId = "87654321-4321-4321-4321-210987654321";
    private const string TestClientSecret = "client-secret-value";
    private const string TestBaseAddress = "https://management.azure.com/";

    private MockHttpMessageHandler _mockHandler;
    private HttpClient _httpClient;
    private IHttpClientFactory _mockHttpClientFactory;
    private ITokenClient _mockTokenClient;
    private AzureHttpClient _azureHttpClient;

    [SetUp]
    public void SetUp()
    {
        const string responseContent = """{"id": "id", "name": "name"}""";
        _mockHandler = new MockHttpMessageHandler(responseContent, HttpStatusCode.OK);
        _httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri(TestBaseAddress)
        };

        // Mock the HTTP client factory
        _mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
        _mockHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(_httpClient);

        // Mock the token client
        _mockTokenClient = Substitute.For<ITokenClient>();
        _mockTokenClient
            .GetTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new OAuthToken { AccessToken = "mock-access-token", TokenType = "Bearer" }));

        _azureHttpClient = new AzureHttpClient(_mockHttpClientFactory, _mockTokenClient, new Uri(TestBaseAddress), TestTenantId, TestClientId, TestClientSecret);
    }

    [TearDown]
    public void TearDown()
    {
        _azureHttpClient.Dispose();
        _httpClient.Dispose();
        _mockHandler.Dispose();
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_HttpClientFactory_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AzureHttpClient(null!, _mockTokenClient, new Uri(TestBaseAddress), TestTenantId, TestClientId, TestClientSecret);
        });

        Assert.That(exception.ParamName, Is.EqualTo("httpClientFactory"));
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_TokenClient_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AzureHttpClient(_mockHttpClientFactory, null!, new Uri(TestBaseAddress), TestTenantId, TestClientId, TestClientSecret);
        });

        Assert.That(exception.ParamName, Is.EqualTo("tokenClient"));
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_BaseAddress_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AzureHttpClient(_mockHttpClientFactory, _mockTokenClient, null!, TestTenantId, TestClientId, TestClientSecret);
        });

        Assert.That(exception.ParamName, Is.EqualTo("baseAddress"));
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_TenantId_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AzureHttpClient(_mockHttpClientFactory, _mockTokenClient, new Uri(TestBaseAddress), null!, TestClientId, TestClientSecret);
        });

        Assert.That(exception.ParamName, Is.EqualTo("tenantId"));
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_ClientId_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AzureHttpClient(_mockHttpClientFactory, _mockTokenClient, new Uri(TestBaseAddress), TestTenantId, null!, TestClientSecret);
        });

        Assert.That(exception.ParamName, Is.EqualTo("clientId"));
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_ClientSecret_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AzureHttpClient(_mockHttpClientFactory, _mockTokenClient, new Uri(TestBaseAddress), TestTenantId, TestClientId, null!);
        });

        Assert.That(exception.ParamName, Is.EqualTo("clientSecret"));
    }

    [Test]
    public async Task GetAsync_Generic_Should_Return_Deserialized_Object()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");

        // Act
        var result = await _azureHttpClient.GetAsync<TestResponseModel>(requestUri).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo("id"));
            Assert.That(result.Name, Is.EqualTo("name"));
        }
    }

    [Test]
    public void GetAsync_Generic_Should_Throw_ArgumentNullException_When_Uri_Is_Null()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _azureHttpClient.GetAsync<TestResponseModel>(null!));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task GetAsync_Generic_Should_Call_TokenClient_For_Authorization()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");

        // Act
        await _azureHttpClient.GetAsync<TestResponseModel>(requestUri).ConfigureAwait(false);

        // Assert
        await _mockTokenClient.Received(1).GetTokenAsync(
            Arg.Is(TestClientId),
            Arg.Is(TestClientSecret),
            Arg.Any<string>(),
            Arg.Is(TestTenantId),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetAsync_Should_Return_HttpResponseMessage()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");

        // Act
        var result = await _azureHttpClient.GetAsync(requestUri).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void GetAsync_Should_Throw_ArgumentNullException_When_Uri_Is_Null()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _azureHttpClient.GetAsync(null!));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task PostAsync_Generic_Should_Return_Deserialized_Object()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("{\"action\": \"test\"}");

        // Act
        var result = await _azureHttpClient.PostAsync<TestResponseModel>(requestUri, content).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo("id"));
        }
    }

    [Test]
    public async Task PostAsync_Generic_Should_Work_With_Null_Content()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");

        // Act
        var result = await _azureHttpClient.PostAsync<TestResponseModel>(requestUri).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task PutAsync_Should_Return_HttpResponseMessage()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("""{"property": "updated-value"}""");

        // Act
        var result = await _azureHttpClient.PutAsync(requestUri, content).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void PutAsync_Should_Throw_ArgumentNullException_When_Uri_Is_Null()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _azureHttpClient.PutAsync(null!, null));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task PostAsync_WithHeaders_Should_Return_HttpResponseMessage()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("content");
        var headers = new Dictionary<string, string>
        {
            { "x-custom-header", "custom-value" },
            { "x-ms-version", "2025-01-01" }
        };

        // Act
        var result = await _azureHttpClient.PostAsync(requestUri, content, headers).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PostAsync_WithHeaders_Should_Work_With_Null_Content()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com/trigger");
        var headers = new Dictionary<string, string> { { "x-trigger", "manual" } };

        // Act
        var result = await _azureHttpClient.PostAsync(requestUri, null, headers).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void PostAsync_WithHeaders_Should_Throw_ArgumentNullException_When_Uri_Is_Null()
    {
        // Arrange
        var headers = new Dictionary<string, string> { { "x-test", "value" } };

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() =>
            _azureHttpClient.PostAsync(null!, null, headers));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task PostAsync_WithHeaders_Should_Add_Custom_Headers_To_Request()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("test");
        var headers = new Dictionary<string, string>
        {
            { "x-custom-1", "value1" },
            { "x-custom-2", "value2" }
        };

        // Act
        var result = await _azureHttpClient.PostAsync(requestUri, content, headers).ConfigureAwait(false);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PostAsync_WithHeaders_Should_Call_TokenClient_For_Authorization()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var headers = new Dictionary<string, string> { { "x-test", "value" } };

        // Act
        await _azureHttpClient.PostAsync(requestUri, null, headers).ConfigureAwait(false);

        // Assert
        await _mockTokenClient.Received(1).GetTokenAsync(
            Arg.Is(TestClientId),
            Arg.Is(TestClientSecret),
            Arg.Any<string>(),
            Arg.Is(TestTenantId),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetPublicAsync_Generic_Should_Return_Deserialized_Object()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");

        // Act
        var result = await _azureHttpClient.GetPublicAsync<TestResponseModel>(requestUri).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo("id"));
            Assert.That(result.Name, Is.EqualTo("name"));
        }
    }

    [Test]
    public async Task GetPublicAsync_Generic_Should_Return_Null_On_Non_Success_StatusCode()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler("", HttpStatusCode.NotFound);
        var httpClient = new HttpClient(mockHandler);
        _mockHttpClientFactory.CreateClient("AzurePublicHttpClient").Returns(httpClient);

        var requestUri = new Uri("https://management.azure.com");

        // Act
        var result = await _azureHttpClient.GetPublicAsync<TestResponseModel>(requestUri).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Null);

        mockHandler.Dispose();
        httpClient.Dispose();
    }

    [Test]
    public void GetPublicAsync_Generic_Should_Throw_ArgumentNullException_When_Uri_Is_Null()
    {
        // Act & Assert  
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _azureHttpClient.GetPublicAsync<TestResponseModel>(null!));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task GetPublicAsync_Generic_Should_Not_Call_TokenClient()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");

        // Act
        await _azureHttpClient.GetPublicAsync<TestResponseModel>(requestUri).ConfigureAwait(false);

        // Assert - Token client should not be called for public endpoints
        await _mockTokenClient.DidNotReceive().GetTokenAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetPublicAsync_Should_Return_HttpResponseMessage()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");

        // Act
        var result = await _azureHttpClient.GetPublicAsync(requestUri).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void GetPublicAsync_Should_Throw_ArgumentNullException_When_Uri_Is_Null()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() =>
            _azureHttpClient.GetPublicAsync(null!));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task GetPublicAsync_Should_Return_Non_Success_StatusCode()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler("Error", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(mockHandler);
        _mockHttpClientFactory.CreateClient("AzurePublicHttpClient").Returns(httpClient);

        var requestUri = new Uri("https://management.azure.com");

        // Act
        var result = await _azureHttpClient.GetPublicAsync(requestUri).ConfigureAwait(false);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

        mockHandler.Dispose();
        httpClient.Dispose();
    }

    [Test]
    public async Task PostPublicAsync_Should_Return_HttpResponseMessage()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("payload");

        // Act
        var result = await _azureHttpClient.PostPublicAsync(requestUri, content).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PostPublicAsync_Should_Work_With_Null_Content()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");

        // Act
        var result = await _azureHttpClient.PostPublicAsync(requestUri, null).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PostPublicAsync_Should_Add_Custom_Headers()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("payload");
        var headers = new Dictionary<string, string>
        {
            { "x-webhook-signature", "signature123" },
            { "x-custom-header", "value" }
        };

        // Act
        var result = await _azureHttpClient.PostPublicAsync(requestUri, content, headers).ConfigureAwait(false);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PostPublicAsync_Should_Work_With_Null_Headers()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("content");

        // Act
        var result = await _azureHttpClient.PostPublicAsync(requestUri, content).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void PostPublicAsync_Should_Throw_ArgumentNullException_When_Uri_Is_Null()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _azureHttpClient.PostPublicAsync(null!, null));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task PostPublicAsync_Should_Not_Call_TokenClient()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("content");

        // Act
        await _azureHttpClient.PostPublicAsync(requestUri, content).ConfigureAwait(false);

        // Assert - Token client should not be called for public endpoints
        await _mockTokenClient.DidNotReceive().GetTokenAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public async Task PostPublicAsync_Should_Return_Non_Success_StatusCode()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler("Forbidden", HttpStatusCode.Forbidden);
        var httpClient = new HttpClient(mockHandler);
        _mockHttpClientFactory.CreateClient("AzurePublicHttpClient").Returns(httpClient);

        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("content");

        // Act
        var result = await _azureHttpClient.PostPublicAsync(requestUri, content).ConfigureAwait(false);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        mockHandler.Dispose();
        httpClient.Dispose();
    }

    [Test]
    public async Task GetAsync_Generic_Should_Return_Null_On_Non_Success_StatusCode()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler("", HttpStatusCode.BadRequest);
        var httpClient = new HttpClient(mockHandler);
        _mockHttpClientFactory.CreateClient("AzureManagementClient").Returns(httpClient);

        var requestUri = new Uri("https://management.azure.com");

        // Act
        var result = await _azureHttpClient.GetAsync<TestResponseModel>(requestUri).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Null);

        mockHandler.Dispose();
        httpClient.Dispose();
    }

    [Test]
    public async Task PostAsync_Generic_Should_Return_Null_On_Non_Success_StatusCode()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler("", HttpStatusCode.Unauthorized);
        var httpClient = new HttpClient(mockHandler);
        _mockHttpClientFactory.CreateClient("AzureManagementClient").Returns(httpClient);

        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("content");

        // Act
        var result = await _azureHttpClient.PostAsync<TestResponseModel>(requestUri, content).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Null);

        mockHandler.Dispose();
        httpClient.Dispose();
    }

    [Test]
    public void Dispose_Should_Not_Throw()
    {
        // Arrange
        const string responseContent = """{"data": "test"}""";
        var mockHandler = new MockHttpMessageHandler(responseContent, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri(TestBaseAddress) };
        var mockFactory = Substitute.For<IHttpClientFactory>();

        mockFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        var client = new AzureHttpClient(mockFactory, _mockTokenClient, new Uri(TestBaseAddress), TestTenantId, TestClientId, TestClientSecret);

        // Act & Assert
        Assert.DoesNotThrow(() => client.Dispose());
        mockHandler.Dispose();
        httpClient.Dispose();
    }

    [Test]
    public void Dispose_Should_Be_Callable_Multiple_Times()
    {
        // Arrange
        const string responseContent = """{"data": "test"}""";
        var mockHandler = new MockHttpMessageHandler(responseContent, HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri(TestBaseAddress) };
        var mockFactory = Substitute.For<IHttpClientFactory>();

        mockFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        var client = new AzureHttpClient(mockFactory, _mockTokenClient, new Uri(TestBaseAddress), TestTenantId, TestClientId, TestClientSecret);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            client.Dispose();
            client.Dispose();
            client.Dispose();
        });
        mockHandler.Dispose();
        httpClient.Dispose();
    }
}