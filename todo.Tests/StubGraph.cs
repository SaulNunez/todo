using System.Net;
using System.Text;
using Microsoft.Graph;

namespace todo.Tests;

/// <summary>
/// A stand-in for the Microsoft Graph endpoint. It records every request so tests can
/// assert on what actually went over the wire, and replies with canned JSON.
/// </summary>
public class StubGraph : HttpMessageHandler
{
    public record Request(HttpMethod Method, string Url, string Body);

    public List<Request> Requests { get; } = new();

    /// <summary>Produces the response body for a request. Defaults to an empty object.</summary>
    public Func<HttpRequestMessage, string> Responder { get; set; } = _ => "{}";

    public IEnumerable<Request> PatchRequests => Requests.Where(r => r.Method == HttpMethod.Patch);
    public IEnumerable<Request> PostRequests => Requests.Where(r => r.Method == HttpMethod.Post);
    public IEnumerable<Request> GetRequests => Requests.Where(r => r.Method == HttpMethod.Get);

    /// <summary>Replies with each body in turn, so paged responses can be scripted.</summary>
    public void RespondInSequence(params string[] bodies)
    {
        var call = 0;
        Responder = _ => bodies[Math.Min(call++, bodies.Length - 1)];
    }

    public ApiQueries CreateApiQueries() => new(new GraphServiceClient(new HttpClient(this)));

    public TodoActions CreateTodoActions() => new(CreateApiQueries());

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new Request(request.Method, request.RequestUri!.ToString(), body));

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Responder(request), Encoding.UTF8, "application/json")
        };
    }
}
