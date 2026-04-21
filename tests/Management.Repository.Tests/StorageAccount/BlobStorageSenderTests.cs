using System.Net;
using LogicApps.Management.Repository.StorageAccount;
using NSubstitute;
using NUnit.Framework;

namespace LogicApps.Management.Repository.Tests.StorageAccount;

[TestFixture]
internal sealed class BlobStorageSenderTests
{
    private IAzureManagementRepository _mockRepository;
    private BlobStorageSender _sender;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = Substitute.For<IAzureManagementRepository>();
        _sender = new BlobStorageSender(_mockRepository);
    }

    [Test]
    public async Task UploadAsync_Should_Call_Repository_PutAsync_With_Correct_Path()
    {
        // Arrange
        const string container = "test-container";
        const string fileName = "test-file.txt";
        var content = new StringContent("test content");
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act
        await _sender.UploadAsync(container, fileName, content).ConfigureAwait(false);

        // Assert
        await _mockRepository.Received(1).PutAsync(
            Arg.Is<Uri>(uri => uri.ToString() == $"{container}/{fileName}"),
            Arg.Is(content)).ConfigureAwait(false);
    }

    [Test]
    public void UploadAsync_Should_Succeed_When_Response_Is_Created()
    {
        // Arrange
        const string container = "container";
        const string fileName = "file.txt";
        var content = new StringContent("content");
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _sender.UploadAsync(container, fileName, content).ConfigureAwait(false));
    }

    [Test]
    public void UploadAsync_Should_Succeed_When_Response_Is_OK()
    {
        // Arrange
        const string container = "container";
        const string fileName = "file.txt";
        var content = new StringContent("content");
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _mockRepository
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _sender.UploadAsync(container, fileName, content).ConfigureAwait(false));
    }

    [Test]
    public void UploadAsync_Should_Throw_HttpRequestException_On_BadRequest()
    {
        // Arrange
        const string container = "container";
        const string fileName = "file.txt";
        var content = new StringContent("content");
        var failureResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request"
        };

        _mockRepository
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await _sender.UploadAsync(container, fileName, content).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to upload blob"));
            Assert.That(exception.Message, Does.Contain("400"));
            Assert.That(exception.Message, Does.Contain("Bad request"));
        }
    }

    [Test]
    public void UploadAsync_Should_Throw_HttpRequestException_On_Unauthorized()
    {
        // Arrange
        const string container = "container";
        const string fileName = "file.txt";
        var content = new StringContent("content");
        var failureResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            ReasonPhrase = "Unauthorized"
        };

        _mockRepository
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await _sender.UploadAsync(container, fileName, content).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to upload blob"));
            Assert.That(exception.Message, Does.Contain("401"));
            Assert.That(exception.Message, Does.Contain("Authorization failure"));
        }
    }

    [Test]
    public void UploadAsync_Should_Throw_HttpRequestException_On_Forbidden()
    {
        // Arrange
        const string container = "container";
        const string fileName = "file.txt";
        var content = new StringContent("content");
        var failureResponse = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            ReasonPhrase = "Forbidden"
        };

        _mockRepository
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await _sender.UploadAsync(container, fileName, content).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to upload blob"));
            Assert.That(exception.Message, Does.Contain("403"));
            Assert.That(exception.Message, Does.Contain("Not authorized to overwrite blob"));
        }
    }

    [Test]
    public void UploadAsync_Should_Throw_HttpRequestException_On_Conflict()
    {
        // Arrange
        const string container = "container";
        const string fileName = "file.txt";
        var content = new StringContent("content");
        var failureResponse = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            ReasonPhrase = "Conflict"
        };

        _mockRepository
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await _sender.UploadAsync(container, fileName, content).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to upload blob"));
            Assert.That(exception.Message, Does.Contain("409"));
            Assert.That(exception.Message, Does.Contain("Blob already exists or conflict occurred"));
        }
    }

    [Test]
    public void UploadAsync_Should_Throw_HttpRequestException_On_InternalServerError()
    {
        // Arrange
        const string container = "container";
        const string fileName = "file.txt";
        var content = new StringContent("content");
        var failureResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            ReasonPhrase = "Internal Server Error"
        };

        _mockRepository
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _sender.UploadAsync(container, fileName, content).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to upload blob"));
            Assert.That(exception.Message, Does.Contain("500"));
            Assert.That(exception.Message, Does.Contain("Internal error"));
        }
    }

    [Test]
    public void UploadAsync_Should_Throw_HttpRequestException_On_Unexpected_Status_Code()
    {
        // Arrange
        const string container = "container";
        const string fileName = "file.txt";
        var content = new StringContent("content");
        var failureResponse = new HttpResponseMessage(HttpStatusCode.NotImplemented)
        {
            ReasonPhrase = "Not Implemented"
        };

        _mockRepository
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(failureResponse));

        // Act & Assert
        var exception = Assert.ThrowsAsync<HttpRequestException>(async () => await _sender.UploadAsync(container, fileName, content).ConfigureAwait(false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.Message, Does.Contain("Failed to upload blob"));
            Assert.That(exception.Message, Does.Contain("501"));
            Assert.That(exception.Message, Does.Contain("Unexpected error"));
        }
    }

    [Test]
    public async Task UploadAsync_Should_Work_With_Nested_Container_Path()
    {
        // Arrange
        const string container = "parent/child/container";
        const string fileName = "file.txt";
        var content = new StringContent("content");
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act
        await _sender.UploadAsync(container, fileName, content).ConfigureAwait(false);

        // Assert
        await _mockRepository.Received(1).PutAsync(
            Arg.Is<Uri>(uri => uri.ToString() == $"{container}/{fileName}"),
            Arg.Any<HttpContent>()).ConfigureAwait(false);
    }

    [Test]
    public async Task UploadAsync_Should_Work_With_File_Name_With_Extension()
    {
        // Arrange
        const string container = "container";
        const string fileName = "test-file.json";
        var content = new StringContent("{}");
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _mockRepository
            .PutAsync(Arg.Any<Uri>(), Arg.Any<HttpContent>())
            .Returns(Task.FromResult(successResponse));

        // Act
        await _sender.UploadAsync(container, fileName, content).ConfigureAwait(false);

        // Assert
        await _mockRepository.Received(1).PutAsync(
            Arg.Is<Uri>(uri => uri.ToString().EndsWith(".json")),
            Arg.Any<HttpContent>()).ConfigureAwait(false);
    }
}