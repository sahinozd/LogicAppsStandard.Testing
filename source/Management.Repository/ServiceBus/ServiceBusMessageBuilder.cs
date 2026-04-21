using Newtonsoft.Json;

namespace LogicApps.Management.Repository.ServiceBus;

public class ServiceBusMessageBuilder
{
    private object? _payload;
    private readonly Dictionary<string, string> _properties = new();
    private string? _correlationId;
    private string? _messageType;

    private ServiceBusMessageBuilder() { }

    public static ServiceBusMessageBuilder Create() => new();

    /// <summary>
    /// Configures the message to use a claim check pattern by referencing an external file location.
    /// </summary>
    /// <remarks>Use this method to offload large payloads to external storage and include only a reference in the message.
    /// This can help reduce message size and improve performance when handling large files.
    /// Add the sender identity as an application property via <see cref="AddProperty"/>.</remarks>
    /// <param name="fileName">The path or identifier of the external file to be referenced by the message. Cannot be null or empty.</param>
    /// <returns>The current instance of the ServiceBusMessageBuilder with the claim check configuration applied.</returns>
    public ServiceBusMessageBuilder WithClaimCheck(string fileName)
    {
        _payload = new
        {
            fileLocation = fileName
        };

        return this;
    }

    /// <summary>
    /// Sets the raw message payload to be sent with the service bus message.
    /// </summary>
    /// <param name="message">The raw message content to use as the payload. Cannot be null.</param>
    /// <returns>The current instance of the builder with the updated message payload.</returns>
    public ServiceBusMessageBuilder WithMessage(string message)
    {
        _payload = message;
        return this;
    }

    /// <summary>
    /// Sets the correlation identifier for the message being built.
    /// </summary>
    /// <remarks>The correlation identifier is typically used to relate this message to other messages or operations for tracking and diagnostic purposes.</remarks>
    /// <param name="correlationId">The correlation identifier to associate with the message. Can be null or empty if no correlation is required.</param>
    /// <returns>The current instance of the builder with the correlation identifier set.</returns>
    public ServiceBusMessageBuilder WithCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    /// <summary>
    /// Sets the message type for the service bus message being built.
    /// </summary>
    /// <param name="messageType">The type of the message to assign. This value is used to categorize or identify the message for consumers.</param>
    /// <returns>The current instance of the builder with the specified message type set.</returns>
    public ServiceBusMessageBuilder WithMessageType(string messageType)
    {
        _messageType = messageType;
        return this;
    }

    /// <summary>
    /// Adds a custom application property to the message being built.
    /// </summary>
    /// <remarks>If a property with the same key already exists, its value is overwritten.</remarks>
    /// <param name="key">The name of the property to add. Cannot be null or empty.</param>
    /// <param name="value">The value of the property to add. Can be null.</param>
    /// <returns>The current instance of the builder, allowing for method chaining.</returns>
    public ServiceBusMessageBuilder AddProperty(string key, string value)
    {
        _properties[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the message payload and associated properties for transmission.
    /// </summary>
    /// <remarks>The returned message payload is serialized as JSON if it is not already a string.
    /// The properties dictionary may include additional metadata such as message type and broker properties,
    /// depending on the configuration prior to calling this method.</remarks>
    /// <returns>A tuple containing the serialized message payload as a string and a dictionary of message properties.
    /// The properties dictionary may include entries such as "messageType" and "BrokerProperties" if they have been set.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the message payload has not been defined before calling this method.</exception>
    public (string Message, Dictionary<string, string> Properties) Build()
    {
        if (_payload == null)
        {
            throw new InvalidOperationException("Message payload is not defined.");
        }

        // Serialize payload 
        var message = _payload as string ?? JsonConvert.SerializeObject(_payload);

        // Add messageType 
        if (!string.IsNullOrEmpty(_messageType))
        {
            _properties["messageType"] = _messageType;
        }

        // Add BrokerProperties 
        if (string.IsNullOrEmpty(_correlationId))
        {
            return (message, _properties);
        }

        var brokerProperties = JsonConvert.SerializeObject(new
        {
            CorrelationId = _correlationId
        });

        _properties["BrokerProperties"] = brokerProperties;

        return (message, _properties);
    }
}