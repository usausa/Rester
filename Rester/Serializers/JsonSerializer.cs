namespace Rester.Serializers;

public sealed class JsonSerializer : ISerializer
{
    public static JsonSerializer Default { get; } = new(new JsonSerializerConfig());

    private readonly System.Text.Json.JsonSerializerOptions options;

    public string ContentType { get; }

    public JsonSerializer(JsonSerializerConfig config)
    {
        options = config.Options;
        ContentType = config.ContentType;
    }

    public async ValueTask SerializeAsync<T>(Stream stream, T obj, CancellationToken cancel)
    {
        await System.Text.Json.JsonSerializer.SerializeAsync(stream, obj, obj!.GetType(), options, cancel).ConfigureAwait(false);
    }

    public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancel)
    {
        return System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream, options, cancel);
    }
}
