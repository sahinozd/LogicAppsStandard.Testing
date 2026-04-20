using System.Net;
using LogicApps.Management.Models.Constants;
using NUnit.Framework;

namespace LogicApps.Management.Tests;

[TestFixture]
internal sealed class WorkflowTriggerExecutionResponseTests
{
    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_HttpResponseMessage_Is_Null()
    {
        // Act & Assert
        var argumentNullException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new WorkflowTriggerExecutionResponse(null!);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(argumentNullException, Is.Not.Null);
            Assert.That(argumentNullException.ParamName, Is.EqualTo("httpResponseMessage"));
        }
    }

    [Test]
    public void Constructor_Should_Initialize_With_Valid_HttpResponseMessage()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response, Is.Not.Null);
    }

    [Test]
    public void ClientTrackingId_Should_Return_Header_Value()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();
        httpResponseMessage.Headers.Add(StringConstants.ClientTrackingIdHeader, "client-tracking-123");

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.ClientTrackingId, Is.EqualTo("client-tracking-123"));
    }

    [Test]
    public void RequestId_Should_Return_Header_Value()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();
        httpResponseMessage.Headers.Add(StringConstants.RequestIdHeader, "request-id-456");

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.RequestId, Is.EqualTo("request-id-456"));
    }

    [Test]
    public void StatusCode_Should_Return_HttpStatusCode()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void StatusCode_Should_Return_Accepted_Status()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage(HttpStatusCode.Accepted);

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
    }

    [Test]
    public void StatusCode_Should_Return_BadRequest_Status()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage(HttpStatusCode.BadRequest);

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public void TrackingId_Should_Return_Header_Value()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();
        httpResponseMessage.Headers.Add(StringConstants.TrackingIdHeader, "tracking-id-789");

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.TrackingId, Is.EqualTo("tracking-id-789"));
    }

    [Test]
    public void TriggerHistoryName_Should_Return_Header_Value()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();
        httpResponseMessage.Headers.Add(StringConstants.TriggerHistoryNameHeader, "trigger-history-abc");

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.TriggerHistoryName, Is.EqualTo("trigger-history-abc"));
    }

    [Test]
    public void WorkFlowRunId_Should_Return_Header_Value()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();
        httpResponseMessage.Headers.Add(StringConstants.WorkflowRunIdHeader, "run-id-def");

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.WorkFlowRunId, Is.EqualTo("run-id-def"));
    }

    [Test]
    public void WorkflowName_Should_Return_Header_Value()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();
        httpResponseMessage.Headers.Add(StringConstants.WorkflowNameHeader, "workflow");

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.WorkflowName, Is.EqualTo("workflow"));
    }

    [Test]
    public void WorkflowVersion_Should_Return_Header_Value()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();
        httpResponseMessage.Headers.Add(StringConstants.WorkflowVersionHeader, "1.0.0");

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.WorkflowVersion, Is.EqualTo("1.0.0"));
    }

    [Test]
    public void All_Properties_Should_Return_Correct_Values()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage(HttpStatusCode.Accepted);
        httpResponseMessage.Headers.Add(StringConstants.ClientTrackingIdHeader, "client-tracking-xyz");
        httpResponseMessage.Headers.Add(StringConstants.RequestIdHeader, "request-id-xyz");
        httpResponseMessage.Headers.Add(StringConstants.TrackingIdHeader, "tracking-id-xyz");
        httpResponseMessage.Headers.Add(StringConstants.TriggerHistoryNameHeader, "trigger-history-xyz");
        httpResponseMessage.Headers.Add(StringConstants.WorkflowRunIdHeader, "run-id-xyz");
        httpResponseMessage.Headers.Add(StringConstants.WorkflowNameHeader, "complete-workflow");
        httpResponseMessage.Headers.Add(StringConstants.WorkflowVersionHeader, "2.0.0");

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.ClientTrackingId, Is.EqualTo("client-tracking-xyz"));
            Assert.That(response.RequestId, Is.EqualTo("request-id-xyz"));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(response.TrackingId, Is.EqualTo("tracking-id-xyz"));
            Assert.That(response.TriggerHistoryName, Is.EqualTo("trigger-history-xyz"));
            Assert.That(response.WorkFlowRunId, Is.EqualTo("run-id-xyz"));
            Assert.That(response.WorkflowName, Is.EqualTo("complete-workflow"));
            Assert.That(response.WorkflowVersion, Is.EqualTo("2.0.0"));
        }
    }

    [Test]
    public void Properties_Should_Return_First_Value_When_Multiple_Header_Values_Present()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();
        string[] headerValues = ["first-value", "second-value"];
        httpResponseMessage.Headers.Add(StringConstants.ClientTrackingIdHeader, headerValues);

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.ClientTrackingId, Is.EqualTo("first-value"));
    }

    [Test]
    public void ClientTrackingId_Should_Throw_ArgumentNullException_When_Header_Missing()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();

        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ = response.ClientTrackingId);
    }

    [Test]
    public void RequestId_Should_Throw_ArgumentNullException_When_Header_Missing()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();

        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ = response.RequestId);
    }

    [Test]
    public void TrackingId_Should_Throw_ArgumentNullException_When_Header_Missing()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();

        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ = response.TrackingId);
    }

    [Test]
    public void TriggerHistoryName_Should_Throw_ArgumentNullException_When_Header_Missing()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();

        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ = response.TriggerHistoryName);
    }

    [Test]
    public void WorkFlowRunId_Should_Throw_ArgumentNullException_When_Header_Missing()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();

        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ = response.WorkFlowRunId);
    }

    [Test]
    public void WorkflowName_Should_Throw_ArgumentNullException_When_Header_Missing()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();

        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ = response.WorkflowName);
    }

    [Test]
    public void WorkflowVersion_Should_Throw_ArgumentNullException_When_Header_Missing()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();

        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _ = response.WorkflowVersion);
    }

    [Test]
    public void StatusCode_Should_Return_InternalServerError()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage(HttpStatusCode.InternalServerError);

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    [Test]
    public void StatusCode_Should_Return_NotFound()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage(HttpStatusCode.NotFound);

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public void StatusCode_Should_Return_Unauthorized()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage(HttpStatusCode.Unauthorized);

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public void Headers_Should_Be_Case_Sensitive()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();
        httpResponseMessage.Headers.Add(StringConstants.ClientTrackingIdHeader, "correct-value");

        // Act
        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Assert
        Assert.That(response.ClientTrackingId, Is.EqualTo("correct-value"));
    }

    [Test]
    public void Multiple_Properties_Can_Be_Accessed_Multiple_Times()
    {
        // Arrange
        using var httpResponseMessage = CreateHttpResponseMessage();
        httpResponseMessage.Headers.Add(StringConstants.ClientTrackingIdHeader, "tracking-123");
        httpResponseMessage.Headers.Add(StringConstants.WorkflowRunIdHeader, "run-456");

        var response = new WorkflowTriggerExecutionResponse(httpResponseMessage);

        // Act & Assert - Access properties multiple times
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.ClientTrackingId, Is.EqualTo("tracking-123"));
            Assert.That(response.WorkFlowRunId, Is.EqualTo("run-456"));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }

    private static HttpResponseMessage CreateHttpResponseMessage(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode);
    }
}
