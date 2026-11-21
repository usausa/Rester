namespace Rester;

using Rester.Internal;

public sealed class PooledBufferStreamTests
{
    private const int PayloadSize = 200 * 1024;

    [Fact]
    public void CopyToWritesRemainingBytesOnce()
    {
        // Arrange
        var payload = MakePayload();
        using var stream = new PooledBufferStream();
        stream.Write(payload);
        stream.Position = 0;
        using var destination = new CountingStream();

        // Act
        stream.CopyTo(destination);

        // Assert
        Assert.Equal(1, destination.Writes);
        Assert.Equal(payload, destination.ToArray());
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public async Task CopyToAsyncWritesRemainingBytesOnce()
    {
        // Arrange
        var payload = MakePayload();
        var stream = new PooledBufferStream();
        var destination = new CountingStream();
        await using (stream.ConfigureAwait(true))
        await using (destination.ConfigureAwait(true))
        {
            stream.Write(payload);
            stream.Position = 0;

            // Act
            await stream.CopyToAsync(destination, TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Assert
            Assert.Equal(1, destination.Writes);
            Assert.Equal(payload, destination.ToArray());
            Assert.Equal(stream.Length, stream.Position);
        }
    }

    [Fact]
    public void CopyToFromMiddlePositionCopiesTail()
    {
        // Arrange
        var payload = MakePayload();
        using var stream = new PooledBufferStream();
        stream.Write(payload);
        stream.Position = PayloadSize / 2;
        using var destination = new CountingStream();

        // Act
        stream.CopyTo(destination);

        // Assert
        Assert.Equal(payload.AsSpan(PayloadSize / 2).ToArray(), destination.ToArray());
    }

    [Fact]
    public async Task CopyToAsyncHonorsCanceledToken()
    {
        // Arrange
        var payload = MakePayload();
        var stream = new PooledBufferStream();
        var destination = new CountingStream();
        await using (stream.ConfigureAwait(true))
        await using (destination.ConfigureAwait(true))
        {
            stream.Write(payload);
            stream.Position = 0;
            using var cancel = new CancellationTokenSource();
            await cancel.CancelAsync().ConfigureAwait(true);

            // Act
            Exception? caught = null;
            try
            {
                await stream.CopyToAsync(destination, cancel.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException e)
            {
                caught = e;
            }

            // Assert
            Assert.NotNull(caught);
            Assert.Equal(0, destination.Writes);
            Assert.Equal(0, stream.Position);
        }
    }

    [Fact]
    public void CopyToAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange
        var stream = new PooledBufferStream();
        stream.Write(MakePayload());
        stream.Position = 0;
        stream.Dispose();
        using var destination = new CountingStream();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => stream.CopyTo(destination));
    }

    private static byte[] MakePayload()
    {
        var payload = new byte[PayloadSize];
        for (var i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)(i % 251);
        }

        return payload;
    }

    private sealed class CountingStream : Stream
    {
        private readonly MemoryStream inner = new();

        public int Writes { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => throw new NotSupportedException();
        }

        public byte[] ToArray() => inner.ToArray();

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            Writes++;
            inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Writes++;
            inner.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Writes++;
            return inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Writes++;
            return inner.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
