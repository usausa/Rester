namespace Rester;

using Rester.Serializers;

public sealed class RestConfig
{
    public static RestConfig Default { get; } = new();

    public ISerializer Serializer { get; set; } = default!;

    public int TransferBufferSize { get; set; }

    public bool PostContentStreaming { get; set; } = true;

    public Func<ILengthResolveContext, long?>? LengthResolver { get; set; }

    public string DefaultUploadContentType { get; set; } = "application/octet-stream";

    public RestConfig()
    {
        TransferBufferSize = 16 * 1024;
    }
}
