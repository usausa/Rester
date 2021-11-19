namespace Rester.Serializers;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class JsonSerializerConfig
{
    public string ContentType { get; set; } = "application/json";

    public JsonSerializerSettings Settings { get; } = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
}
