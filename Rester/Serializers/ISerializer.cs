namespace Rester.Serializers;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

public interface ISerializer
{
    string ContentType { get; }

    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization requires dynamic code.")]
    ValueTask SerializeAsync<T>(Stream stream, T obj, CancellationToken cancel);

    ValueTask SerializeAsync<T>(Stream stream, T obj, JsonTypeInfo<T> typeInfo, CancellationToken cancel);

    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization requires dynamic code.")]
    ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancel);

    ValueTask<T?> DeserializeAsync<T>(Stream stream, JsonTypeInfo<T> typeInfo, CancellationToken cancel);
}
