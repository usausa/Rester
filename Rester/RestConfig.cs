namespace Rester;

using Rester.Serializers;

public class RestConfig
{
    public static RestConfig Default { get; } = new();

    public ISerializer Serializer { get; set; } = default!;

    public int TransferBufferSize { get; set; } = 16 * 1024;

    public Func<ILengthResolveContext, long?>? LengthResolver { get; set; }
}
