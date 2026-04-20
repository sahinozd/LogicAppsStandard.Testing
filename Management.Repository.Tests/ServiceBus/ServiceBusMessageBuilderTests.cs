using LogicApps.Management.Repository.ServiceBus;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LogicApps.Management.Repository.Tests.ServiceBus;

[TestFixture]
internal sealed class ServiceBusMessageBuilderTests
{
    [Test]
    public void Create_Should_Return_New_Instance()
    {
        // Act
        var builder = ServiceBusMessageBuilder.Create();

        // Assert
        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void Build_Should_Throw_When_Payload_Not_Set()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.That(exception.Message, Does.Contain("Message payload is not defined"));
    }

    [Test]
    public void WithClaimCheck_Should_Set_Payload_With_FileLocation_And_Sender_As_Property()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string fileName = "file.json";
        const string sender = "sender";

        // Act
        var (builderMessage, properties) = builder
            .WithClaimCheck(fileName)
            .AddProperty("sender", sender)
            .Build();

        // Assert
        var payload = JObject.Parse(builderMessage);
        Assert.That(payload["fileLocation"]?.ToString(), Is.EqualTo(fileName));
        Assert.That(properties["sender"], Is.EqualTo(sender));
    }

    [Test]
    public void WithRawMessage_Should_Set_Payload_As_String()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string message = "This is a message";

        // Act
        var (builderMessage, _) = builder
            .WithMessage(message)
            .Build();

        // Assert
        Assert.That(builderMessage, Is.EqualTo(message));
    }

    [Test]
    public void WithCorrelationId_Should_Add_BrokerProperties_With_CorrelationId()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string correlationId = "correlation-id";
        const string message = "message";

        // Act
        var (_, properties) = builder
            .WithMessage(message)
            .WithCorrelationId(correlationId)
            .Build();

        // Assert
        Assert.That(properties, Does.ContainKey("BrokerProperties"));
        var brokerProperties = JObject.Parse(properties["BrokerProperties"]);
        Assert.That(brokerProperties["CorrelationId"]?.ToString(), Is.EqualTo(correlationId));
    }

    [Test]
    public void WithMessageType_Should_Add_MessageType_Property()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string messageType = "messageType";
        const string message = "message";

        // Act
        var (_, properties) = builder
            .WithMessage(message)
            .WithMessageType(messageType)
            .Build();

        // Assert
        Assert.That(properties, Does.ContainKey("messageType"));
        Assert.That(properties["messageType"], Is.EqualTo(messageType));
    }

    [Test]
    public void AddProperty_Should_Add_Custom_Property()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string key = "key";
        const string value = "value";
        const string message = "message";

        // Act
        var (_, properties) = builder
            .WithMessage(message)
            .AddProperty(key, value)
            .Build();

        // Assert
        Assert.That(properties, Does.ContainKey(key));
        Assert.That(properties[key], Is.EqualTo(value));
    }

    [Test]
    public void AddProperty_Should_Overwrite_Existing_Property()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string key = "key";
        const string value1 = "value-1";
        const string value2 = "value-2";
        const string message = "message";

        // Act
        var (_, properties) = builder
            .WithMessage(message)
            .AddProperty(key, value1)
            .AddProperty(key, value2)
            .Build();

        // Assert
        Assert.That(properties[key], Is.EqualTo(value2));
    }

    [Test]
    public void Build_Should_Return_Empty_Properties_When_Nothing_Set()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string message = "message";

        // Act
        var (_, properties) = builder
            .WithMessage(message)
            .Build();

        // Assert
        Assert.That(properties, Is.Empty);
    }

    [Test]
    public void Build_Should_Not_Add_BrokerProperties_When_CorrelationId_Is_Empty()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string message = "message";

        // Act
        var (_, properties) = builder
            .WithMessage(message)
            .WithCorrelationId(string.Empty)
            .Build();

        // Assert
        Assert.That(properties, Does.Not.ContainKey("BrokerProperties"));
    }

    [Test]
    public void Build_Should_Not_Add_MessageType_When_MessageType_Is_Empty()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string message = "message";

        // Act
        var (_, properties) = builder
            .WithMessage(message)
            .WithMessageType(string.Empty)
            .Build();

        // Assert
        Assert.That(properties, Does.Not.ContainKey("messageType"));
    }

    [Test]
    public void Build_Should_Support_Method_Chaining()
    {
        // Arrange & Act
        var (message, properties) = ServiceBusMessageBuilder
            .Create()
            .WithMessage("test")
            .WithCorrelationId("corr-123")
            .WithMessageType("TestMessage")
            .AddProperty("prop1", "value1")
            .AddProperty("prop2", "value2")
            .Build();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(message, Is.EqualTo("test"));
            Assert.That(properties, Does.ContainKey("BrokerProperties"));
            Assert.That(properties, Does.ContainKey("messageType"));
            Assert.That(properties, Does.ContainKey("prop1"));
            Assert.That(properties, Does.ContainKey("prop2"));
        }
    }

    [Test]
    public void WithClaimCheck_Should_Serialize_Payload_As_Json()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string fileName = "file.json";

        // Act
        var (builderMessage, _) = builder
            .WithClaimCheck(fileName)
            .Build();

        // Assert
        Assert.That(() => JObject.Parse(builderMessage), Throws.Nothing);
        var payload = JObject.Parse(builderMessage);
        Assert.That(payload["fileLocation"], Is.Not.Null);
    }

    [Test]
    public void Build_Should_Include_All_Properties_When_Everything_Is_Set()
    {
        // Arrange
        const string fileName = "test.json";
        const string sender = "sender";
        const string correlationId = "correlation-id";
        const string messageType = "messageType";
        const string customKey = "environment";
        const string customValue = "production";

        // Act
        var (message, properties) = ServiceBusMessageBuilder
            .Create()
            .WithClaimCheck(fileName)
            .WithCorrelationId(correlationId)
            .WithMessageType(messageType)
            .AddProperty("sender", sender)
            .AddProperty(customKey, customValue)
            .Build();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            // Message
            var payload = JObject.Parse(message);
            Assert.That(payload["fileLocation"]?.ToString(), Is.EqualTo(fileName));

            // Properties
            Assert.That(properties, Does.ContainKey("BrokerProperties"));
            Assert.That(properties, Does.ContainKey("messageType"));
            Assert.That(properties, Does.ContainKey("sender"));
            Assert.That(properties, Does.ContainKey(customKey));
            Assert.That(properties["sender"], Is.EqualTo(sender));

            // BrokerProperties content
            var brokerProps = JObject.Parse(properties["BrokerProperties"]);
            Assert.That(brokerProps["CorrelationId"]?.ToString(), Is.EqualTo(correlationId));

            // Other properties
            Assert.That(properties["messageType"], Is.EqualTo(messageType));
            Assert.That(properties[customKey], Is.EqualTo(customValue));
        }
    }

    [Test]
    public void WithRawMessage_Should_Override_Previous_Payload()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string fileName = "file.json";
        const string message = "message";

        // Act
        var (builderMessage, _) = builder
            .WithClaimCheck(fileName)
            .WithMessage(message)  // Should override claim check
            .Build();

        // Assert
        Assert.That(builderMessage, Is.EqualTo(message));
    }

    [Test]
    public void WithClaimCheck_Should_Override_Previous_Payload()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string message = "message";
        const string fileName = "file.json";

        // Act
        var (builderMessage, _) = builder
            .WithMessage(message)
            .WithClaimCheck(fileName)  // Should override message
            .Build();

        // Assert
        var payload = JObject.Parse(builderMessage);
        Assert.That(payload["fileLocation"]?.ToString(), Is.EqualTo(fileName));
    }

    [Test]
    public void Build_Should_Handle_Null_CorrelationId()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string message = "message";

        // Act
        var (_, properties) = builder
            .WithMessage(message)
            .WithCorrelationId(null!)
            .Build();

        // Assert
        Assert.That(properties, Does.Not.ContainKey("BrokerProperties"));
    }

    [Test]
    public void Build_Should_Handle_Null_MessageType()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string message = "message";

        // Act
        var (_, properties) = builder
            .WithMessage(message)
            .WithMessageType(null!)
            .Build();

        // Assert
        Assert.That(properties, Does.Not.ContainKey("messageType"));
    }

    [Test]
    public void Build_Should_Handle_Complex_Json_In_RawMessage()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string complexJson = """{"name":"Test","values":[1,2,3],"nested":{"key":"value"}}""";

        // Act
        var (builderMessage, _) = builder
            .WithMessage(complexJson)
            .Build();

        // Assert
        Assert.That(builderMessage, Is.EqualTo(complexJson));
        Assert.That(() => JObject.Parse(builderMessage), Throws.Nothing);
    }

    [Test]
    public void AddProperty_Should_Handle_Empty_String_Value()
    {
        // Arrange
        var builder = ServiceBusMessageBuilder.Create();
        const string key = "emptyKey";
        const string value = "";

        // Act
        var (_, properties) = builder
            .WithMessage("message")
            .AddProperty(key, value)
            .Build();

        // Assert
        Assert.That(properties[key], Is.EqualTo(string.Empty));
    }
}