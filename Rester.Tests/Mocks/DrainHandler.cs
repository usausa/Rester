namespace Rester.Mocks;

using System.Net;

internal sealed class DrainHandler : HttpMessageHandler
{
    private readonly Action? onSend;

    public DrainHandler(Action? onSend = null)
    {
        this.onSend = onSend;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        onSend?.Invoke();

        if (request.Content is not null)
        {
            await request.Content.CopyToAsync(Stream.Null, cancellationToken).ConfigureAwait(false);
        }

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}
