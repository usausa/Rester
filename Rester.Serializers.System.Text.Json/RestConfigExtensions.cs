namespace Rester
{
    using System;

    using Rester.Serializers;

    public static class RestConfigExtensions
    {
        public static RestConfig UseJsonSerializer(this RestConfig config)
        {
            config.Serializer = JsonSerializer.Default;
            return config;
        }

        public static RestConfig UseJsonSerializer(this RestConfig config, Action<System.Text.Json.JsonSerializerOptions> action)
        {
            var serializerConfig = new JsonSerializerConfig();
            action(serializerConfig.Options);
            config.Serializer = new JsonSerializer(serializerConfig);
            return config;
        }
    }
}
