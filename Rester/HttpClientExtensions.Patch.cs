namespace Rester;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json.Serialization.Metadata;

public static partial class HttpClientExtensions
{
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization requires dynamic code.")]
    public static ValueTask<IRestResponse> PatchAsync(
        this HttpClient client,
        string path,
        object parameter,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return client.PatchAsync(RestConfig.Default, path, parameter, headers, compress, cancel);
    }

    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization requires dynamic code.")]
    public static ValueTask<IRestResponse> PatchAsync(
        this HttpClient client,
        RestConfig config,
        string path,
        object parameter,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return SendContentCoreAsync(client, config, HttpMethod.Patch, path, parameter, headers, compress, cancel);
    }

    [RequiresUnreferencedCode("JSON serialization/deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization/deserialization requires dynamic code.")]
    public static ValueTask<IRestResponse<T>> PatchAsync<T>(
        this HttpClient client,
        string path,
        object parameter,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return client.PatchAsync<T>(RestConfig.Default, path, parameter, headers, compress, cancel);
    }

    [RequiresUnreferencedCode("JSON serialization/deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization/deserialization requires dynamic code.")]
    public static ValueTask<IRestResponse<T>> PatchAsync<T>(
        this HttpClient client,
        RestConfig config,
        string path,
        object parameter,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return SendContentCoreAsync<T>(client, config, HttpMethod.Patch, path, parameter, headers, compress, cancel);
    }

    public static ValueTask<IRestResponse> PatchAsync<TRequest>(
        this HttpClient client,
        string path,
        TRequest parameter,
        JsonTypeInfo<TRequest> requestTypeInfo,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return client.PatchAsync(RestConfig.Default, path, parameter, requestTypeInfo, headers, compress, cancel);
    }

    public static ValueTask<IRestResponse> PatchAsync<TRequest>(
        this HttpClient client,
        RestConfig config,
        string path,
        TRequest parameter,
        JsonTypeInfo<TRequest> requestTypeInfo,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return SendContentCoreAsync(client, config, HttpMethod.Patch, path, parameter, requestTypeInfo, headers, compress, cancel);
    }

    public static ValueTask<IRestResponse<TResponse>> PatchAsync<TRequest, TResponse>(
        this HttpClient client,
        string path,
        TRequest parameter,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<TResponse> responseTypeInfo,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return client.PatchAsync(RestConfig.Default, path, parameter, requestTypeInfo, responseTypeInfo, headers, compress, cancel);
    }

    public static ValueTask<IRestResponse<TResponse>> PatchAsync<TRequest, TResponse>(
        this HttpClient client,
        RestConfig config,
        string path,
        TRequest parameter,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<TResponse> responseTypeInfo,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return SendContentCoreAsync(client, config, HttpMethod.Patch, path, parameter, requestTypeInfo, responseTypeInfo, headers, compress, cancel);
    }
}
