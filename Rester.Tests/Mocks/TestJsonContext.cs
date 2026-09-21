namespace Rester.Mocks;

using System.Text.Json.Serialization;

[JsonSerializable(typeof(SingleResponse))]
[JsonSerializable(typeof(PostResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class TestJsonContext : JsonSerializerContext;
