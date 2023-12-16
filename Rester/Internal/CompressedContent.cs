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

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
#pragma warning disable CA2000
        var compressedStream = compress == CompressOption.Gzip
            ? (Stream)new GZipStream(stream, CompressionMode.Compress, true)
            : new DeflateStream(stream, CompressionMode.Compress, true);
#pragma warning restore CA2000
#pragma warning disable CA2008
        return content.CopyToAsync(compressedStream, context)
            .ContinueWith(_ => compressedStream.Dispose());
#pragma warning restore CA2008
    }
}
