namespace Rester.Serializers;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

public sealed class JsonSerializer : ISerializer
{
    public static JsonSerializer Default { get; } = new(new JsonSerializerConfig());

    private readonly JsonSerializerOptions options;

    public string ContentType { get; }

    public JsonSerializer(JsonSerializerConfig config)
    {
        options = config.Options;
        ContentType = config.ContentType;
    }

    public JsonSerializer(JsonSerializerOptions options, string contentType = "application/json")
    {
        this.options = options;
        ContentType = contentType;
    }

    public JsonSerializer(JsonSerializerContext context, string contentType = "application/json")
    {
        options = new JsonSerializerOptions(context.Options);
        ContentType = contentType;
    }

    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization requires dynamic code.")]
    public async ValueTask SerializeAsync<T>(Stream stream, T obj, CancellationToken cancel)
    {
        await System.Text.Json.JsonSerializer.SerializeAsync(stream, obj, options, cancel).ConfigureAwait(false);
    }

    public async ValueTask SerializeAsync<T>(Stream stream, T obj, JsonTypeInfo<T> typeInfo, CancellationToken cancel)
    {
        await System.Text.Json.JsonSerializer.SerializeAsync(stream, obj, typeInfo, cancel).ConfigureAwait(false);
    }

    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization requires dynamic code.")]
    public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancel)
    {
        return System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream, options, cancel);
    }

    public ValueTask<T?> DeserializeAsync<T>(Stream stream, JsonTypeInfo<T> typeInfo, CancellationToken cancel)
    {
        return System.Text.Json.JsonSerializer.DeserializeAsync(stream, typeInfo, cancel);
    }
}
