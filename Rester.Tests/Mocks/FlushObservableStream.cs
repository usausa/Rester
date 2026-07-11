namespace Rester.Mocks;

internal sealed class FlushObservableStream : MemoryStream
{
    public int FlushCount { get; private set; }

    public override void Flush()
    {
        FlushCount++;
        base.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        FlushCount++;
        return base.FlushAsync(cancellationToken);
    }
}
