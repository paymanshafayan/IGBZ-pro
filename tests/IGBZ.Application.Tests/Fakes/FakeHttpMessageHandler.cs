namespace IGBZ.Application.Tests.Fakes;

using System.Net;
using System.Text;
using System.Text.Json;

/// <summary>HttpClient ساختگی — پاسخ‌های قابل‌کنترل بدون شبکه (نسخهٔ مشترک برای تست‌های Application).</summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private HttpResponseMessage? _response;
    public string? LastBody { get; private set; }

    public void RespondJson(int statusCode, object body)
    {
        _response = new HttpResponseMessage((HttpStatusCode)statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            LastBody = request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
        }
        return Task.FromResult(_response ?? new HttpResponseMessage(HttpStatusCode.OK));
    }
}
