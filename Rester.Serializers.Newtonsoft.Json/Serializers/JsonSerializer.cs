namespace Rester.Serializers
{
    using System.IO;

    using Newtonsoft.Json;

    public sealed class JsonSerializer : ISerializer
    {
        public static JsonSerializer Default { get; } = new JsonSerializer(new JsonSerializerConfig());

        private readonly Newtonsoft.Json.JsonSerializer serializer;

        public string ContentType { get; }

        public JsonSerializer(JsonSerializerConfig config)
        {
            serializer = Newtonsoft.Json.JsonSerializer.Create(config.Settings);
            ContentType = config.ContentType;
        }

        public void Serialize(Stream stream, object obj)
        {
            var sw = new StreamWriter(stream);
            var jtw = new JsonTextWriter(sw);
            serializer.Serialize(jtw, obj);
            jtw.Flush();
        }

        public T Deserialize<T>(Stream stream)
        {
            var sr = new StreamReader(stream);
            var jtr = new JsonTextReader(sr);
            return serializer.Deserialize<T>(jtr);
        }
    }
}
