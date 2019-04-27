namespace Rester.Serializers
{
    using System.IO;

    public interface ISerializer
    {
        string ContentType { get; }

        void Serialize(Stream stream, object obj);

        T Deserialize<T>(Stream stream);
    }
}
