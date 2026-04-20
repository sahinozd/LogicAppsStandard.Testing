using System.Net;
using LogicApps.Management.Models.Constants;

namespace LogicApps.Management;

/// <summary>
/// Wraps an <see cref="HttpResponseMessage"/> returned when executing a workflow trigger and exposes commonly
/// used headers and the response status code as strongly-typed properties.
/// </summary>
public class WorkflowTriggerExecutionResponse(HttpResponseMessage? httpResponseMessage)
{
    private readonly HttpResponseMessage _httpResponseMessage = httpResponseMessage ?? throw new ArgumentNullException(nameof(httpResponseMessage));

    /// <summary>
    /// Gets the client tracking id from the response headers.
    /// </summary>
    public string ClientTrackingId => _httpResponseMessage.Headers.FirstOrDefault((i) => i.Key == StringConstants.ClientTrackingIdHeader).Value.First();

    /// <summary>
    /// Gets the request identifier returned by the service.
    /// </summary>
    public string RequestId => _httpResponseMessage.Headers.FirstOrDefault((i) => i.Key == StringConstants.RequestIdHeader).Value.First();

    /// <summary>
    /// Gets the HTTP status code of the response.
    /// </summary>
    public HttpStatusCode StatusCode => _httpResponseMessage.StatusCode;

    /// <summary>
    /// Gets the tracking id header value from the response.
    /// </summary>
    public string TrackingId => _httpResponseMessage.Headers.FirstOrDefault((i) => i.Key == StringConstants.TrackingIdHeader).Value.First();

    /// <summary>
    /// Gets the trigger history name assigned by the service for this execution.
    /// </summary>
    public string TriggerHistoryName => _httpResponseMessage.Headers.FirstOrDefault((i) => i.Key == StringConstants.TriggerHistoryNameHeader).Value.First();

    /// <summary>
    /// Gets the workflow run id associated with this trigger execution.
    /// </summary>
    public string WorkFlowRunId => _httpResponseMessage.Headers.FirstOrDefault((i) => i.Key == StringConstants.WorkflowRunIdHeader).Value.First();

    /// <summary>
    /// Gets the workflow name returned in the response headers.
    /// </summary>
    public string WorkflowName => _httpResponseMessage.Headers.FirstOrDefault((i) => i.Key == StringConstants.WorkflowNameHeader).Value.First();

    /// <summary>
    /// Gets the workflow version reported by the service in the response headers.
    /// </summary>
    public string WorkflowVersion => _httpResponseMessage.Headers.FirstOrDefault((i) => i.Key == StringConstants.WorkflowVersionHeader).Value.First();
}