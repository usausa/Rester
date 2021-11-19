namespace Rester.Internal;

using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

internal sealed class UploadStreamContent : HttpContent
{
    public static Func<Stream, Stream, Func<Stream, Stream, ValueTask>, ValueTask> Gzip => async (source, destination, task) =>
    {
#pragma warning disable CA2007
        await using var compressedStream = (Stream)new GZipStream(destination, CompressionMode.Compress, true);
#pragma warning restore CA2007
        await task(source, compressedStream).ConfigureAwait(false);
    };

    public static Func<Stream, Stream, Func<Stream, Stream, ValueTask>, ValueTask> Deflate => async (source, destination, task) =>
    {
#pragma warning disable CA2007
        await using var compressedStream = (Stream)new DeflateStream(destination, CompressionMode.Compress, true);
#pragma warning restore CA2007
        await task(source, compressedStream).ConfigureAwait(false);
    };

    private readonly Stream source;

    private readonly int bufferSize;

    private readonly CompressOption compress;

    private readonly Action<long>? progress;

    private readonly CancellationToken cancel;

    public UploadStreamContent(Stream source, int bufferSize, CompressOption compress, Action<long>? progress, CancellationToken cancel)
    {
        this.source = source;
        this.bufferSize = bufferSize;
        this.compress = compress;
        this.progress = progress;
        this.cancel = cancel;

        if (compress != CompressOption.None)
        {
            Headers.ContentEncoding.Add(compress.ToString().ToLowerInvariant());
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            source.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override bool TryComputeLength(out long length)
    {
        if ((compress == CompressOption.None) && source.CanSeek)
        {
            length = source.Length;
            return true;
        }

        length = 0;
        return false;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        var filter = ResolveFilter(compress);
        if (progress is null)
        {
            if (filter is null)
            {
                await source.CopyToAsync(stream, bufferSize, cancel).ConfigureAwait(false);
            }
            else
            {
                await filter(source, stream, async (s, d) => await s.CopyToAsync(d, bufferSize, cancel).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }
        else
        {
            if (filter is null)
            {
                await CopyAsync(source, stream, bufferSize, progress, cancel).ConfigureAwait(false);
            }
            else
            {
                await filter(source, stream, (s, d) => CopyAsync(s, d, bufferSize, progress, cancel)).ConfigureAwait(false);
            }
        }
    }

    private static Func<Stream, Stream, Func<Stream, Stream, ValueTask>, ValueTask>? ResolveFilter(CompressOption compress)
    {
        return compress switch
        {
            CompressOption.Gzip => Gzip,
            CompressOption.Deflate => Deflate,
            _ => null
        };
    }

    private static async ValueTask CopyAsync(Stream source, Stream destination, int bufferSize, Action<long> progress, CancellationToken cancel)
    {
        var buffer = new byte[bufferSize];
        int read;
        while ((read = await source.ReadAsync(buffer, cancel).ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancel).ConfigureAwait(false);
            progress(read);
        }

        await destination.FlushAsync(cancel).ConfigureAwait(false);
    }
}
