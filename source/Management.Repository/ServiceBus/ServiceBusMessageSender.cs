using System.Net;
using System.Text;

namespace LogicApps.Management.Repository.ServiceBus;

/// <summary>
/// Provides functionality for sending messages to Azure Service Bus queues or topics using an underlying Azure management repository.
/// </summary>
/// <remarks>This class abstracts the process of sending messages to Azure Service Bus, allowing messages to be sent asynchronously to a specified queue or topic.
/// It handles message serialization and error reporting based on HTTP response status codes.
/// Thread safety depends on the implementation of the provided repository.</remarks>
/// <param name="repository">The Azure management repository used to perform HTTP operations against the Service Bus endpoint. Cannot be null.</param>
public class ServiceBusMessageSender(IAzureManagementRepository repository) : IServiceBusMessageSender
{
    /// <summary>
    /// Asynchronously sends a message to the specified queue or topic path.
    /// </summary>
    /// <param name="queueOrTopicPath">The relative path of the queue or topic to which the message will be sent. Must not be null or empty.</param>
    /// <param name="message">The message payload to send, formatted as a JSON string. Cannot be null.</param>
    /// <param name="properties">An optional collection of application properties to include with the message. If null, no additional properties are sent.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    /// <exception cref="HttpRequestException">Thrown if the message fails to send due to an unsuccessful HTTP response.</exception>
    public async Task SendAsync(string queueOrTopicPath, string message, Dictionary<string, string>? properties = null)
    {
        using var content = new StringContent(message, Encoding.UTF8, "application/json");
        var path = new Uri($"{queueOrTopicPath}/messages", UriKind.Relative);

        var result = properties == null
            ? await repository.PostAsync(path, content).ConfigureAwait(false)
            : await repository.PostAsync(path, content, properties).ConfigureAwait(false);

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to send message. Status {(int)result.StatusCode}: {result.ReasonPhrase}. {GetErrorMessage(result.StatusCode)}");
        }
    }

    /// <summary>
    /// Returns a user-friendly error message corresponding to the specified HTTP status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code for which to retrieve the error message.</param>
    /// <returns>A string containing the error message associated with the specified status code.
    /// Returns a generic message if the status code is not recognized.</returns>
    private static string GetErrorMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Created => "Message successfully sent.",
        HttpStatusCode.BadRequest => "Bad request.",
        HttpStatusCode.Unauthorized => "Authorization failure.",
        HttpStatusCode.Forbidden => "Quota exceeded or message too large.",
        HttpStatusCode.Gone => "Queue or topic does not exist.",
        HttpStatusCode.InternalServerError => "Internal error.",
        _ => "Unexpected error."
    };
}