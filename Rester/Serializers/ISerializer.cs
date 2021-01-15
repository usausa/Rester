namespace Rester.Serializers
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ISerializer
    {
        string ContentType { get; }

        ValueTask SerializeAsync<T>(Stream stream, T obj, CancellationToken cancel);

        ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancel);
    }
}
