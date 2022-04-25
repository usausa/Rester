namespace Rester.Serializers;

using Newtonsoft.Json;

public sealed class JsonSerializer : ISerializer
{
    public static JsonSerializer Default { get; } = new(new JsonSerializerConfig());

    private readonly Newtonsoft.Json.JsonSerializer serializer;

    public string ContentType { get; }

    public JsonSerializer(JsonSerializerConfig config)
    {
        serializer = Newtonsoft.Json.JsonSerializer.Create(config.Settings);
        ContentType = config.ContentType;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Ignore")]
    public ValueTask SerializeAsync<T>(Stream stream, T obj, CancellationToken cancel)
    {
        var sw = new StreamWriter(stream);
        var jtw = new JsonTextWriter(sw);
        serializer.Serialize(jtw, obj);
        jtw.Flush();
        return default;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Ignore")]
    public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancel)
    {
        var sr = new StreamReader(stream);
        var jtr = new JsonTextReader(sr);
        return new ValueTask<T?>(serializer.Deserialize<T>(jtr));
    }
}
