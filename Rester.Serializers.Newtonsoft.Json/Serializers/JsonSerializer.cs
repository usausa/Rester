namespace Rester.Serializers
{
    using Newtonsoft.Json;

    public sealed class JsonSerializer : ISerializer
    {
        public static JsonSerializer Default { get; } = new JsonSerializer(new JsonSerializerConfig());

        private readonly JsonSerializerSettings settings;

        public string ContentType { get; }

        public JsonSerializer(JsonSerializerConfig config)
        {
            settings = config.Settings;
            ContentType = config.ContentType;
        }

        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, settings);
        }

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, settings);
        }
    }
}
