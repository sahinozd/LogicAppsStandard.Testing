using System.Net;
using System.Text;

namespace LogicApps.Management.Repository.Tests;

/// <summary>
/// Mock HTTP message handler for testing HTTP client behavior without real network calls.
/// </summary>
internal sealed class MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode) : HttpMessageHandler
{
    private readonly List<HttpRequestMessage> _requests = [];

    public IReadOnlyList<HttpRequestMessage> Requests => _requests.AsReadOnly();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request);

        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        });
    }
}
