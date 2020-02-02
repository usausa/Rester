namespace Rester.Serializers
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class JsonSerializer : ISerializer
    {
        public static JsonSerializer Default { get; } = new JsonSerializer(new JsonSerializerConfig());

        private readonly System.Text.Json.JsonSerializerOptions options;

        public string ContentType { get; }

        public JsonSerializer(JsonSerializerConfig config)
        {
            options = config.Options;
            ContentType = config.ContentType;
        }

        public async ValueTask SerializeAsync<T>(Stream stream, T obj, CancellationToken cancel)
        {
            await System.Text.Json.JsonSerializer.SerializeAsync(stream, obj, options, cancel).ConfigureAwait(false);
        }

        public async ValueTask<T> DeserializeAsync<T>(Stream stream, CancellationToken cancel)
        {
            return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(stream, options, cancel);
        }
    }
}
