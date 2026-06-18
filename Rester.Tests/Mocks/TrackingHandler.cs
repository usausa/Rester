namespace Rester.Mocks;

internal sealed class TrackingHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, TrackingResponse> factory;

    public TrackingResponse? LastResponse { get; private set; }

    public TrackingHandler(Func<HttpRequestMessage, TrackingResponse> factory)
    {
        this.factory = factory;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastResponse = factory(request);
        return Task.FromResult<HttpResponseMessage>(LastResponse);
    }
}
