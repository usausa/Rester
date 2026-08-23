namespace Rester.Internal;

using System.Buffers;

internal sealed class PooledBufferStream : Stream
{
    private const int DefaultCapacity = 4096;

    private byte[] buffer;

    private int length;

    private int position;

    private bool disposed;

    public override bool CanRead => !disposed;

    public override bool CanSeek => !disposed;

    public override bool CanWrite => !disposed;

    public override long Length => length;

    public override long Position
    {
        get => position;
        set => position = (int)value;
    }

    public PooledBufferStream(int capacity = DefaultCapacity)
    {
        buffer = ArrayPool<byte>.Shared.Rent(capacity);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            disposed = true;
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = [];
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Read
    //--------------------------------------------------------------------------------

    public override int Read(byte[] target, int offset, int count)
    {
        return Read(target.AsSpan(offset, count));
    }

    public override int Read(Span<byte> target)
    {
        var remain = length - position;
        if (remain <= 0)
        {
            return 0;
        }

        var count = Math.Min(remain, target.Length);
        buffer.AsSpan(position, count).CopyTo(target);
        position += count;
        return count;
    }

    public override Task<int> ReadAsync(byte[] target, int offset, int count, CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<int>(cancellationToken)
            : Task.FromResult(Read(target.AsSpan(offset, count)));
    }

    public override ValueTask<int> ReadAsync(Memory<byte> target, CancellationToken cancellationToken = default)
    {
        return cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled<int>(cancellationToken)
            : new ValueTask<int>(Read(target.Span));
    }

    public override int ReadByte()
    {
        return position < length ? buffer[position++] : -1;
    }

    //--------------------------------------------------------------------------------
    // Write
    //--------------------------------------------------------------------------------

    public override void Write(byte[] source, int offset, int count)
    {
        Write(source.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> source)
    {
        EnsureCapacity(position + source.Length);
        source.CopyTo(buffer.AsSpan(position));
        position += source.Length;
        if (position > length)
        {
            length = position;
        }
    }

    public override Task WriteAsync(byte[] source, int offset, int count, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        Write(source.AsSpan(offset, count));
        return Task.CompletedTask;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        Write(source.Span);
        return ValueTask.CompletedTask;
    }

    public override void WriteByte(byte value)
    {
        EnsureCapacity(position + 1);
        buffer[position++] = value;
        if (position > length)
        {
            length = position;
        }
    }

    //--------------------------------------------------------------------------------
    // Seek
    //--------------------------------------------------------------------------------

    public override long Seek(long offset, SeekOrigin origin)
    {
        var target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End => length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        position = (int)target;
        return position;
    }

    public override void SetLength(long value)
    {
        var target = (int)value;
        EnsureCapacity(target);
        if (target > length)
        {
            buffer.AsSpan(length, target - length).Clear();
        }

        length = target;
        if (position > length)
        {
            position = length;
        }
    }

    public override void Flush()
    {
    }

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

    //--------------------------------------------------------------------------------
    // Grow
    //--------------------------------------------------------------------------------

    private void EnsureCapacity(int required)
    {
        if (required > buffer.Length)
        {
            Grow(required);
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void Grow(int required)
    {
        var newBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(required, buffer.Length * 2));
        buffer.AsSpan(0, length).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = newBuffer;
    }
}
