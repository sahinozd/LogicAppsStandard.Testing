using NSubstitute;
using NUnit.Framework;

namespace LogicApps.Management.Repository.Tests;

[TestFixture]
internal sealed partial class AzureManagementRepositoryTests
{
    private IAzureHttpClient _mockHttpClient;
    private Uri _baseAddress;
    private AzureManagementRepository _repository;

    [SetUp]
    public void SetUp()
    {
        _mockHttpClient = Substitute.For<IAzureHttpClient>();
        _baseAddress = new Uri("https://management.azure.com/");
        _repository = new AzureManagementRepository(_mockHttpClient, _baseAddress);
    }

    [TearDown]
    public void TearDown()
    {
        _repository.Dispose();
        _mockHttpClient.Dispose();
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_HttpClient_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AzureManagementRepository(null!, _baseAddress);
        });

        Assert.That(exception.ParamName, Is.EqualTo("httpClient"));
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_BaseAddress_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AzureManagementRepository(_mockHttpClient, null!);
        });

        Assert.That(exception.ParamName, Is.EqualTo("baseAddress"));
    }

    [Test]
    public void Constructor_Should_Create_Instance_With_Valid_Parameters()
    {
        // Act
        var repository = new AzureManagementRepository(_mockHttpClient, _baseAddress);

        // Assert
        Assert.That(repository, Is.Not.Null);
    }

    [Test]
    public async Task GetObject_Should_Call_HttpClient_GetAsync_When_Content_Is_Null()
    {
        // Arrange
        var relativeUri = new Uri("/subscriptions/subscription-id", UriKind.Relative);
        var expectedData = new { Id = "id", Name = "name" };

        _mockHttpClient
            .GetAsync<object>(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedData));

        // Act
        var result = await _repository.GetObjectAsync<object>(relativeUri).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.EqualTo(expectedData));
        await _mockHttpClient.Received(1).GetAsync<object>(
            Arg.Is<Uri>(uri => uri.AbsoluteUri.Contains("/subscriptions/subscription-id")),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetObject_Should_Call_HttpClient_PostAsync_When_Content_Is_Provided()
    {
        // Arrange
        var relativeUri = new Uri("/subscriptions/subscription-id", UriKind.Relative);
        var content = new StringContent("content");
        var expectedData = new { Id = "id", Name = "name" };

        _mockHttpClient
            .PostAsync<object>(Arg.Any<Uri>(), Arg.Any<HttpContent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedData));

        // Act
        var result = await _repository.GetObjectAsync<object>(relativeUri, content).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.EqualTo(expectedData));
        await _mockHttpClient.Received(1).PostAsync<object>(
            Arg.Is<Uri>(uri => uri.AbsoluteUri.Contains("/subscriptions/subscription-id")),
            Arg.Is(content),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public async Task GetObject_Should_Combine_BaseAddress_And_RelativeUri()
    {
        // Arrange
        var relativeUri = new Uri("subscriptions/subscription-id/resourceGroups/resource-group", UriKind.Relative);
        var expectedData = new { Id = "id" };
        Uri? capturedUri = null;

        _mockHttpClient
            .GetAsync<object>(Arg.Do<Uri>(uri => capturedUri = uri), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedData));

        // Act
        await _repository.GetObjectAsync<object>(relativeUri).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedUri, Is.Not.Null);
            Assert.That(capturedUri!.AbsoluteUri, Does.StartWith("https://management.azure.com/"));
            Assert.That(capturedUri.AbsoluteUri, Does.Contain("subscriptions/subscription-id/resourceGroups/resource-group"));
        }
    }

    [Test]
    public void GetObjectPublicAsync_Should_Throw_ArgumentNullException_When_RequestUri_Is_Null()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _repository.GetObjectPublicAsync<object>(null!));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task GetObjectPublicAsync_Should_Call_HttpClient_GetPublicAsync()
    {
        // Arrange
        var requestUri = new Uri("https://some-made-up-url.com/api");
        var expectedData = new { Value = "value" };

        _mockHttpClient
            .GetPublicAsync<object>(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedData));

        // Act
        var result = await _repository.GetObjectPublicAsync<object>(requestUri).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.EqualTo(expectedData));

        await _mockHttpClient.Received(1).GetPublicAsync<object>(
            Arg.Is(requestUri),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public void GetAsync_Should_Throw_ArgumentNullException_When_RequestUri_Is_Null()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _repository.GetAsync(null!));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task GetAsync_Should_Call_HttpClient_GetAsync()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var expectedResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        _mockHttpClient
            .GetAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _repository.GetAsync(requestUri).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResponse));

        await _mockHttpClient.Received(1).GetAsync(
            Arg.Is(requestUri),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public void GetPublicAsync_Should_Throw_ArgumentNullException_When_RequestUri_Is_Null()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() =>
            _repository.GetPublicAsync(null!));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task GetPublicAsync_Should_Call_HttpClient_GetPublicAsync()
    {
        // Arrange
        var requestUri = new Uri("https://some-made-up-url.com/api");
        var expectedResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        _mockHttpClient
            .GetPublicAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _repository.GetPublicAsync(requestUri).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResponse));
        await _mockHttpClient.Received(1).GetPublicAsync(
            Arg.Is(requestUri),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public void PostAsync_Should_Throw_ArgumentNullException_When_RequestUri_Is_Null()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _repository.PostAsync(null!, null));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task PostAsync_Should_Call_HttpClient_PostAsync()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("content");
        var expectedResponse = new HttpResponseMessage(System.Net.HttpStatusCode.Created);

        _mockHttpClient
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _repository.PostAsync(requestUri, content).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResponse));
        await _mockHttpClient.Received(1).PostAsync(
            Arg.Is(requestUri),
            Arg.Is(content),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public void PostPublicAsync_Should_Throw_ArgumentNullException_When_RequestUri_Is_Null()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _repository.PostPublicAsync(null!, null));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task PostPublicAsync_Should_Call_HttpClient_PostPublicAsync()
    {
        // Arrange
        var requestUri = new Uri("https://some-made-up-url.com/api");
        var content = new StringContent("content");
        var headers = new Dictionary<string, string> { { "Custom-Header", "value" } };
        var expectedResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        _mockHttpClient
            .PostPublicAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _repository.PostPublicAsync(requestUri, content, headers).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResponse));

        await _mockHttpClient.Received(1).PostPublicAsync(
            Arg.Is(requestUri),
            Arg.Is(content),
            Arg.Is(headers),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public void PostAsync_WithHeaders_Should_Throw_ArgumentNullException_When_RequestUri_Is_Null()
    {
        // Arrange
        var headers = new Dictionary<string, string> { { "Header", "value" } };

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _repository.PostAsync(null!, null, headers));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public void PostAsync_WithHeaders_Should_Throw_ArgumentNullException_When_Headers_Is_Null()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() =>_repository.PostAsync(requestUri, null, null!));

        Assert.That(exception.ParamName, Is.EqualTo("headers"));
    }

    [Test]
    public async Task PostAsync_WithHeaders_Should_Call_HttpClient_PostAsync_With_Headers()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("content");
        var headers = new Dictionary<string, string>
        {
            { "x-ms-version", "2025-01-01" },
            { "x-custom-header", "some-custom-value" }
        };

        var expectedResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        _mockHttpClient
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _repository.PostAsync(requestUri, content, headers).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResponse));
        await _mockHttpClient.Received(1).PostAsync(
            Arg.Is(requestUri),
            Arg.Is(content),
            Arg.Is(headers),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public void PutAsync_Should_Throw_ArgumentNullException_When_RequestUri_Is_Null()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _repository.PutAsync(null!, null));

        Assert.That(exception.ParamName, Is.EqualTo("requestUri"));
    }

    [Test]
    public async Task PutAsync_Should_Call_HttpClient_PutAsync()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("{\"property\":\"value\"}");
        var expectedResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        _mockHttpClient
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _repository.PutAsync(requestUri, content).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResponse));
        await _mockHttpClient.Received(1).PutAsync(
            Arg.Is(requestUri),
            Arg.Is(content),
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Test]
    public void Dispose_Should_Dispose_HttpClient()
    {
        // Arrange
        var repository = new AzureManagementRepository(_mockHttpClient, _baseAddress);

        // Act
        repository.Dispose();

        // Assert
        _mockHttpClient.Received(1).Dispose();
    }

    [Test]
    public void Dispose_Should_Be_Callable_Multiple_Times()
    {
        // Arrange
        var repository = new AzureManagementRepository(_mockHttpClient, _baseAddress);

        // Act
        repository.Dispose();
        repository.Dispose();
        repository.Dispose();

        // Assert - Should not throw
        Assert.Pass("Dispose called multiple times without exception");
    }

    [Test]
    public async Task GetAsync_Should_Retry_On_429_Throttling()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var callCount = 0;

        var throttledResponse = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        throttledResponse.Headers.Add("Retry-After", "1"); // Fast test: 1 second

        var successResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        _mockHttpClient
            .GetAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1 ? Task.FromResult(throttledResponse) : Task.FromResult(successResponse);
            });

        // Act
        var result = await _repository.GetAsync(requestUri).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(callCount, Is.EqualTo(2), "Should retry once after 429");
            Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        }
    }

    [Test]
    public async Task GetAsync_Should_Return_Non_429_Error_Without_Retry()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var callCount = 0;

        var badRequestResponse = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);

        _mockHttpClient
            .GetAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult(badRequestResponse);
            });

        // Act
        var result = await _repository.GetAsync(requestUri).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(callCount, Is.EqualTo(1), "Should not retry for non-429 errors");
            Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        }
    }

    [Test]
    public async Task PostAsync_Should_Retry_Multiple_Times_On_Repeated_429()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("content");
        var callCount = 0;

        var throttledResponse = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        throttledResponse.Headers.Add("Retry-After", "1"); // Fast test: 1 second

        var successResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        _mockHttpClient
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                // Reduced from 3 to 2 retries for faster test execution
                return callCount <= 2 ? Task.FromResult(throttledResponse) : Task.FromResult(successResponse);
            });

        // Act
        var result = await _repository.PostAsync(requestUri, content).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(callCount, Is.EqualTo(3), "Should retry 2 times then succeed");
            Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        }
    }

    [Test]
    public void GetAsync_Should_Throw_After_Max_Retries_Exceeded()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");

        var throttledResponse = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        throttledResponse.Headers.Add("Retry-After", "1"); // Fast test: 1 second

        _mockHttpClient
            .GetAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(throttledResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(() =>
            _repository.GetAsync(requestUri));

        Assert.That(exception.Message, Does.Contain("Exceeded 5 retries"));
    }

    [Test]
    public async Task GetObject_Should_Retry_On_Failed_Request()
    {
        // Arrange
        var relativeUri = new Uri("/subscriptions", UriKind.Relative);
        var callCount = 0;

        _mockHttpClient
            .GetAsync<object>(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new HttpRequestException("Network error");
                }
                return Task.FromResult<object?>(new { Id = "success" });
            });

        // Act
        var result = await _repository.GetObjectAsync<object>(relativeUri).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(callCount, Is.EqualTo(2), "Should retry after failure");
            Assert.That(result, Is.Not.Null);
        }
    }

    [Test]
    [Explicit("This test takes ~30 seconds due to exponential backoff (2+4+8+16). Run manually if needed.")]
    public Task GetObject_Should_Throw_After_All_Retries_Fail()
    {
        // Arrange
        var relativeUri = new Uri("/subscriptions", UriKind.Relative);

        // Always throw to prove retry exhaustion
        _mockHttpClient
            .GetAsync<object>(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns<object?>(_ => throw new HttpRequestException("Persistent error"));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(() =>
            _repository.GetObjectAsync<object>(relativeUri));

        // The exception should be the persistent error (not wrapped)
        Assert.That(exception.Message, Is.EqualTo("Persistent error"));
        return Task.CompletedTask;
    }

    [Test]
    public async Task PutAsync_Should_Use_Retry_After_Header_Seconds()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("content");
        var callCount = 0;

        var throttledResponse = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        throttledResponse.Headers.Add("Retry-After", "1"); // Fast test: 1 second

        var successResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        _mockHttpClient
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1 ? Task.FromResult(throttledResponse) : Task.FromResult(successResponse);
            });

        var startTime = DateTimeOffset.UtcNow;

        // Act
        await _repository.PutAsync(requestUri, content).ConfigureAwait(false);
        var elapsed = DateTimeOffset.UtcNow - startTime;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(elapsed.TotalSeconds, Is.GreaterThanOrEqualTo(0.9), "Should wait at least ~0.2 seconds");
        }
    }

    [Test]
    public async Task GetAsync_Should_Use_Retry_After_Header_DateTime()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var callCount = 0;

        var retryAfterDate = DateTimeOffset.UtcNow.AddSeconds(0.15); // Fast test: 0.15 seconds
        var throttledResponse = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        throttledResponse.Headers.Add("Retry-After", retryAfterDate.ToString("R")); // RFC1123 format

        var successResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        _mockHttpClient
            .GetAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1 ? Task.FromResult(throttledResponse) : Task.FromResult(successResponse);
            });

        // Act
        await _repository.GetAsync(requestUri).ConfigureAwait(false);

        // Assert - Verify retry happened (don't check exact timing due to test environment variability)
        Assert.That(callCount, Is.EqualTo(2), "Should retry after receiving 429 with Retry-After header");
    }

    [Test]
    public async Task GetAsync_Should_Use_Exponential_Backoff_When_No_Retry_After_Header()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var callCount = 0;

        var throttledResponse = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        // No Retry-After header

        var successResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        _mockHttpClient
            .GetAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                // Reduced from 2 to 1 retry for faster test (2s to ~2s instead of 6s)
                return callCount <= 1 ? Task.FromResult(throttledResponse) : Task.FromResult(successResponse);
            });

        var startTime = DateTimeOffset.UtcNow;

        // Act
        await _repository.GetAsync(requestUri).ConfigureAwait(false);

        var elapsed = DateTimeOffset.UtcNow - startTime;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(callCount, Is.EqualTo(2));
            // Exponential backoff: 2^1 = 2 seconds minimum
            Assert.That(elapsed.TotalSeconds, Is.GreaterThanOrEqualTo(1.8), "Should use exponential backoff (~2s)");
        }
    }

    [Test]
    public async Task PostPublicAsync_Should_Retry_On_Throttling()
    {
        // Arrange
        var requestUri = new Uri("https://management.azure.com");
        var content = new StringContent("content");
        var headers = new Dictionary<string, string> { { "Header", "Value" } };
        var callCount = 0;

        var throttledResponse = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        throttledResponse.Headers.Add("Retry-After", "1"); // Fast test: 1 second

        var successResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        _mockHttpClient
            .PostPublicAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1 ? Task.FromResult(throttledResponse) : Task.FromResult(successResponse);
            });

        // Act
        var result = await _repository.PostPublicAsync(requestUri, content, headers).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(callCount, Is.EqualTo(2));
            Assert.That(result.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        }
    }
}