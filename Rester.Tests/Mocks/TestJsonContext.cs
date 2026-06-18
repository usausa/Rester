namespace Rester.Mocks;

using System.Text.Json.Serialization;

[JsonSerializable(typeof(SingleResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class TestJsonContext : JsonSerializerContext;
