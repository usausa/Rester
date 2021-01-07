namespace Rester.Serializers
{
    using System.Text.Json;

    public class JsonSerializerConfig
    {
        public string ContentType { get; set; } = "application/json";

        public JsonSerializerOptions Options { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
