namespace Rester.Mocks;

using System.Text.Json.Serialization;

[JsonSerializable(typeof(PostRequest))]
internal sealed partial class PostJsonContext : JsonSerializerContext;
