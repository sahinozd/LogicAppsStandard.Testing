namespace LogicApps.Management.Repository.ServiceBus;

public interface IServiceBusMessageSender
{
    /// <summary>
    /// Asynchronously sends a message to the specified queue or topic path.
    /// </summary>
    /// <param name="queueOrTopicPath">The relative path of the queue or topic to which the message will be sent. Must not be null or empty.</param>
    /// <param name="message">The message payload to send, formatted as a JSON string. Cannot be null.</param>
    /// <param name="properties">An optional collection of application properties to include with the message. If null, no additional properties are sent.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the message fails to send due to an unsuccessful HTTP response.</exception>
    Task SendAsync(string queueOrTopicPath, string message, Dictionary<string, string>? properties = null);
}