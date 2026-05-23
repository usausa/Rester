namespace Rester;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

using Rester.Serializers;

public static class RestConfigExtensions
{
    public static RestConfig UseJsonSerializer(this RestConfig config)
    {
        config.Serializer = JsonSerializer.Default;
        return config;
    }

    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization requires dynamic code.")]
    public static RestConfig UseJsonSerializer(this RestConfig config, Action<System.Text.Json.JsonSerializerOptions> action)
    {
        var serializerConfig = new JsonSerializerConfig();
        action(serializerConfig.Options);
        config.Serializer = new JsonSerializer(serializerConfig);
        return config;
    }

    public static RestConfig UseJsonSerializer(this RestConfig config, JsonSerializerContext context, string contentType = "application/json")
    {
        config.Serializer = new JsonSerializer(context, contentType);
        return config;
    }
}
