namespace Rester.Internal;

using System.IO.Compression;
using System.Net;
using System.Net.Http;

internal sealed class CompressedContent : HttpContent
{
    private readonly HttpContent content;

    private readonly CompressOption compress;

    public CompressedContent(HttpContent content, CompressOption compress)
    {
        this.content = content;
        this.compress = compress;

        foreach (var header in content.Headers)
        {
            Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        Headers.ContentEncoding.Add(compress.ToString().ToLowerInvariant());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            content.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
#pragma warning disable CA2007
        await using var compressedStream = compress == CompressOption.Gzip
            ? (Stream)new GZipStream(stream, CompressionMode.Compress, true)
            : new DeflateStream(stream, CompressionMode.Compress, true);
#pragma warning restore CA2007
        await content.CopyToAsync(compressedStream, context).ConfigureAwait(false);
    }
}
