namespace Rester.Serializers
{
    public interface ISerializer
    {
        string ContentType { get; }

        string Serialize(object obj);

        T Deserialize<T>(string json);
    }
}
