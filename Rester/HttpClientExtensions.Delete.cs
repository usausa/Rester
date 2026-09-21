namespace Rester;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json.Serialization.Metadata;

public static partial class HttpClientExtensions
{
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization requires dynamic code.")]
    public static ValueTask<IRestResponse<T>> DeleteAsync<T>(
        this HttpClient client,
        string path,
        IDictionary<string, object>? headers = null,
        CancellationToken cancel = default)
    {
        return client.DeleteAsync<T>(RestConfig.Default, path, headers, cancel);
    }

    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization requires dynamic code.")]
    public static ValueTask<IRestResponse<T>> DeleteAsync<T>(
        this HttpClient client,
        RestConfig config,
        string path,
        IDictionary<string, object>? headers = null,
        CancellationToken cancel = default)
    {
        return SendCoreAsync<T>(client, config, HttpMethod.Delete, path, headers, cancel);
    }

    public static ValueTask<IRestResponse<T>> DeleteAsync<T>(
        this HttpClient client,
        string path,
        JsonTypeInfo<T> typeInfo,
        IDictionary<string, object>? headers = null,
        CancellationToken cancel = default)
    {
        return client.DeleteAsync(RestConfig.Default, path, typeInfo, headers, cancel);
    }

    public static ValueTask<IRestResponse<T>> DeleteAsync<T>(
        this HttpClient client,
        RestConfig config,
        string path,
        JsonTypeInfo<T> typeInfo,
        IDictionary<string, object>? headers = null,
        CancellationToken cancel = default)
    {
        return SendCoreAsync(client, config, HttpMethod.Delete, path, typeInfo, headers, cancel);
    }
}
