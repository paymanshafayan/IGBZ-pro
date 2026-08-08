namespace IGBZ.Application.Tests.Fakes;

/// <summary>HttpClientFactory ساختگی که یک HttpClient با handler مشخص برمی‌گرداند.</summary>
public class FakeHttpClientFactoryWithHandler : System.Net.Http.IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public FakeHttpClientFactoryWithHandler(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public System.Net.Http.HttpClient CreateClient(string name) => new(_handler);
}
