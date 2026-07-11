namespace Rester.Mocks;

internal sealed class BrokenRequest
{
    private readonly string message = "Broken.";

    public int Value => throw new InvalidOperationException(message);
}
