namespace Rester.Serializers
{
    using System.Text.Json;

    public class JsonSerializerConfig
    {
        public string ContentType { get; set; } = "application/json";

        public JsonSerializerOptions Settings { get; } = new JsonSerializerOptions();
    }
}
