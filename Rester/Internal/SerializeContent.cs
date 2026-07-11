namespace Rester.Internal;

using System.Net;
using System.Net.Http;

internal sealed class SerializeContent : HttpContent
{
    private readonly Func<Stream, CancellationToken, ValueTask> serialize;

    private readonly CancellationToken cancel;

    public Exception? SerializeError { get; private set; }

    public SerializeContent(Func<Stream, CancellationToken, ValueTask> serialize, CancellationToken cancel)
    {
        this.serialize = serialize;
        this.cancel = cancel;
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        SerializeToStreamAsync(stream, context, cancel);

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        try
        {
            await serialize(stream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            SerializeError = e;
            throw;
        }
    }
}
