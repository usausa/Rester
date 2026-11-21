namespace Rester.Internal;

using System.Buffers;

internal sealed class PooledBufferStream : Stream
{
    private const int DefaultCapacity = 4096;

    private byte[] bytes;

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
        bytes = ArrayPool<byte>.Shared.Rent(capacity);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            disposed = true;
            ArrayPool<byte>.Shared.Return(bytes);
            bytes = [];
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Read
    //--------------------------------------------------------------------------------

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        var remain = length - position;
        if (remain <= 0)
        {
            return 0;
        }

        var count = Math.Min(remain, buffer.Length);
        bytes.AsSpan(position, count).CopyTo(buffer);
        position += count;
        return count;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<int>(cancellationToken)
            : Task.FromResult(Read(buffer.AsSpan(offset, count)));
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled<int>(cancellationToken)
            : new ValueTask<int>(Read(buffer.Span));
    }

    public override int ReadByte()
    {
        return position < length ? bytes[position++] : -1;
    }

    //--------------------------------------------------------------------------------
    // Write
    //--------------------------------------------------------------------------------

    public override void Write(byte[] buffer, int offset, int count)
    {
        Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        EnsureCapacity(position + buffer.Length);
        buffer.CopyTo(bytes.AsSpan(position));
        position += buffer.Length;
        if (position > length)
        {
            length = position;
        }
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        Write(buffer.AsSpan(offset, count));
        return Task.CompletedTask;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    public override void WriteByte(byte value)
    {
        EnsureCapacity(position + 1);
        bytes[position++] = value;
        if (position > length)
        {
            length = position;
        }
    }

    //--------------------------------------------------------------------------------
    // CopyTo
    //--------------------------------------------------------------------------------

    public override void CopyTo(Stream destination, int bufferSize)
    {
        if (destination == this)
        {
            base.CopyTo(destination, bufferSize);
            return;
        }

        ValidateCopyToArguments(destination, bufferSize);
        ObjectDisposedException.ThrowIf(disposed, this);

        var offset = position;
        var count = length - offset;
        if (count > 0)
        {
            position = length;
            destination.Write(bytes, offset, count);
        }
    }

    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        if (destination == this)
        {
            await base.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);
            return;
        }

        ValidateCopyToArguments(destination, bufferSize);
        ObjectDisposedException.ThrowIf(disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var offset = position;
        var count = length - offset;
        if (count > 0)
        {
            position = length;
            await destination.WriteAsync(bytes.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
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
            bytes.AsSpan(length, target - length).Clear();
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
        if (required > bytes.Length)
        {
            Grow(required);
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void Grow(int required)
    {
        var newBytes = ArrayPool<byte>.Shared.Rent(Math.Max(required, bytes.Length * 2));
        bytes.AsSpan(0, length).CopyTo(newBytes);
        ArrayPool<byte>.Shared.Return(bytes);
        bytes = newBytes;
    }
}
