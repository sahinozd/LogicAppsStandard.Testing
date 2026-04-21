using System.Net;
using LogicApps.Management.Repository.ServiceBus;
using NSubstitute;
using NUnit.Framework;

namespace LogicApps.Management.Repository.Tests.ServiceBus;

[TestFixture]
internal sealed class ServiceBusMessageSenderTests
{
    private IAzureManagementRepository _mockRepository;
    private ServiceBusMessageSender _sender;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = Substitute.For<IAzureManagementRepository>();
        _sender = new ServiceBusMessageSender(_mockRepository);
    }

    [Test]
    public async Task SendAsync_Should_Call_Repository_PostAsync_With_Correct_Path_When_No_Properties()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = """{"data":"test"}""";
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act
        await _sender.SendAsync(queuePath, message).ConfigureAwait(false);

        // Assert
        await _mockRepository.Received(1).PostAsync(
            Arg.Is<Uri>(uri => uri.ToString() == $"{queuePath}/messages"),
            Arg.Any<HttpContent>()).ConfigureAwait(false);
    }

    [Test]
    public async Task SendAsync_Should_Call_Repository_PostAsync_With_Properties_When_Provided()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = """{"data":"test"}""";
        var properties = new Dictionary<string, string>
        {
            { "messageType", "messageType" },
            { "priority", "high" }
        };
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>(), Arg.Any<Dictionary<string, string>>())
            .Returns(Task.FromResult(successResponse));

        // Act
        await _sender.SendAsync(queuePath, message, properties).ConfigureAwait(false);

        // Assert
        await _mockRepository.Received(1).PostAsync(
            Arg.Is<Uri>(uri => uri.ToString() == $"{queuePath}/messages"),
            Arg.Any<HttpContent>(),
            Arg.Is<Dictionary<string, string>>(p => p == properties)).ConfigureAwait(false);
    }

    [Test]
    public void SendAsync_Should_Succeed_When_Response_Is_Created()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "message";
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _sender.SendAsync(queuePath, message).ConfigureAwait(false));
    }

    [Test]
    public void SendAsync_Should_Succeed_When_Response_Is_OK()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "message";
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _sender.SendAsync(queuePath, message).ConfigureAwait(false));
    }

    [Test]
    public void SendAsync_Should_Throw_HttpRequestException_On_BadRequest()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "message";
        var failureResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request"
        };

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await _sender.SendAsync(queuePath, message).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to send message"));
            Assert.That(exception.Message, Does.Contain("400"));
            Assert.That(exception.Message, Does.Contain("Bad request"));
        }
    }

    [Test]
    public void SendAsync_Should_Throw_HttpRequestException_On_Unauthorized()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "message";
        var failureResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            ReasonPhrase = "Unauthorized"
        };

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await _sender.SendAsync(queuePath, message).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to send message"));
            Assert.That(exception.Message, Does.Contain("401"));
            Assert.That(exception.Message, Does.Contain("Authorization failure"));
        }
    }

    [Test]
    public void SendAsync_Should_Throw_HttpRequestException_On_Forbidden()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "message";
        var failureResponse = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            ReasonPhrase = "Forbidden"
        };

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await _sender.SendAsync(queuePath, message).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to send message"));
            Assert.That(exception.Message, Does.Contain("403"));
            Assert.That(exception.Message, Does.Contain("Quota exceeded or message too large"));
        }
    }

    [Test]
    public void SendAsync_Should_Throw_HttpRequestException_On_Gone()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "message";
        var failureResponse = new HttpResponseMessage(HttpStatusCode.Gone)
        {
            ReasonPhrase = "Gone"
        };

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await _sender.SendAsync(queuePath, message).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to send message"));
            Assert.That(exception.Message, Does.Contain("410"));
            Assert.That(exception.Message, Does.Contain("Queue or topic does not exist"));
        }
    }

    [Test]
    public void SendAsync_Should_Throw_HttpRequestException_On_InternalServerError()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "message";
        var failureResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            ReasonPhrase = "Internal Server Error"
        };

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await _sender.SendAsync(queuePath, message).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to send message"));
            Assert.That(exception.Message, Does.Contain("500"));
            Assert.That(exception.Message, Does.Contain("Internal error"));
        }
    }

    [Test]
    public void SendAsync_Should_Throw_HttpRequestException_On_Unexpected_Status_Code()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "message";
        var failureResponse = new HttpResponseMessage(HttpStatusCode.NotImplemented)
        {
            ReasonPhrase = "Not Implemented"
        };

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await _sender.SendAsync(queuePath, message).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to send message"));
            Assert.That(exception.Message, Does.Contain("501"));
            Assert.That(exception.Message, Does.Contain("Unexpected error"));
        }
    }

    [Test]
    public async Task SendAsync_Should_Work_With_Topic_Path()
    {
        // Arrange
        const string topicPath = "mytopic";
        const string message = "message";
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act
        await _sender.SendAsync(topicPath, message).ConfigureAwait(false);

        // Assert
        await _mockRepository.Received(1).PostAsync(
            Arg.Is<Uri>(uri => uri.ToString() == $"{topicPath}/messages"),
            Arg.Any<HttpContent>()).ConfigureAwait(false);
    }

    [Test]
    public async Task SendAsync_Should_Append_Messages_To_Path()
    {
        // Arrange
        const string queuePath = "my-queue";
        const string message = "test";
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act
        await _sender.SendAsync(queuePath, message).ConfigureAwait(false);

        // Assert
        await _mockRepository.Received(1).PostAsync(
            Arg.Is<Uri>(uri => uri.ToString().EndsWith("/messages")),
            Arg.Any<HttpContent>()).ConfigureAwait(false);
    }

    [Test]
    public async Task SendAsync_Should_Send_Message_As_Json_Content()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = """{"key":"value"}""";
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);
        string? capturedContentType = null;
        string? capturedCharSet = null;

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Do<HttpContent>(c =>
            {
                capturedContentType = c.Headers.ContentType?.MediaType;
                capturedCharSet = c.Headers.ContentType?.CharSet;
            }))
            .Returns(Task.FromResult(successResponse));

        // Act
        await _sender.SendAsync(queuePath, message).ConfigureAwait(false);

        // Assert
        Assert.That(capturedContentType, Is.EqualTo("application/json"));
        Assert.That(capturedCharSet, Is.EqualTo("utf-8"));
    }

    [Test]
    public async Task SendAsync_Should_Use_UTF8_Encoding()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "Unicode: 你好 مرحبا";
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);
        HttpContent? capturedContent = null;

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Do<HttpContent>(c => capturedContent = c))
            .Returns(Task.FromResult(successResponse));

        // Act
        await _sender.SendAsync(queuePath, message).ConfigureAwait(false);

        // Assert
        Assert.That(capturedContent, Is.Not.Null);
        Assert.That(capturedContent!.Headers.ContentType?.CharSet, Is.EqualTo("utf-8"));
    }

    [Test]
    public void SendAsync_Should_Handle_Empty_Message()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "";
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _sender.SendAsync(queuePath, message).ConfigureAwait(false));
    }

    [Test]
    public async Task SendAsync_Should_Handle_Null_Properties()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "test";
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act
        await _sender.SendAsync(queuePath, message).ConfigureAwait(false);

        // Assert
        await _mockRepository.Received(1).PostAsync(
            Arg.Any<Uri>(),
            Arg.Any<HttpContent>()).ConfigureAwait(false);
    }

    [Test]
    public async Task SendAsync_Should_Handle_Empty_Properties_Dictionary()
    {
        // Arrange
        const string queuePath = "queue";
        const string message = "test";
        var properties = new Dictionary<string, string>();
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PostAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>(), Arg.Any<Dictionary<string, string>>())
            .Returns(Task.FromResult(successResponse));

        // Act
        await _sender.SendAsync(queuePath, message, properties).ConfigureAwait(false);

        // Assert
        await _mockRepository.Received(1).PostAsync(
            Arg.Any<Uri>(),
            Arg.Any<HttpContent>(),
            Arg.Is<Dictionary<string, string>>(p => p.Count == 0)).ConfigureAwait(false);
    }
}