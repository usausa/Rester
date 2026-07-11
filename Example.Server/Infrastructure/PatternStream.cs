namespace Example.Server.Infrastructure;

public sealed class PatternStream : Stream
{
    private readonly long length;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

#pragma warning disable IDE0032
    public override long Length => length;
#pragma warning restore IDE0032

    public override long Position { get; set; }

    public PatternStream(long length)
    {
        this.length = length;
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        var remain = length - Position;
        if (remain <= 0)
        {
            return 0;
        }

        var size = (int)Math.Min(buffer.Length, remain);
        for (var i = 0; i < size; i++)
        {
            buffer[i] = (byte)((Position + i) % 256);
        }

        Position += size;
        return size;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled<int>(cancellationToken)
            : ValueTask.FromResult(Read(buffer.Span));

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            _ => length + offset
        };
        return Position;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
