namespace Rester;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization.Metadata;

using Rester.Internal;

public static partial class HttpClientExtensions
{
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization requires dynamic code.")]
    public static ValueTask<IRestResponse> PostAsync(
        this HttpClient client,
        string path,
        object parameter,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return client.PostAsync(RestConfig.Default, path, parameter, headers, compress, cancel);
    }

    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization requires dynamic code.")]
    public static async ValueTask<IRestResponse> PostAsync(
        this HttpClient client,
        RestConfig config,
        string path,
        object parameter,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        HttpResponseMessage? response = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);
#pragma warning disable CA2007
            await using var stream = new MemoryStream();
#pragma warning restore CA2007

            ProcessHeaders(request, headers);

            try
            {
                await config.Serializer.SerializeAsync(stream, parameter, cancel).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return new RestResponse<object>(RestResult.SerializeError, 0, e, default);
            }

            stream.Seek(0, SeekOrigin.Begin);
#pragma warning disable CA2000
            var content = (HttpContent)new StreamContent(stream);
#pragma warning restore CA2000
            content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
            if (compress != CompressOption.None)
            {
                content = new CompressedContent(content, compress);
            }

            request.Content = content;

            response = await client.SendAsync(request, cancel).ConfigureAwait(false);
            return new RestResponse<object>(response.IsSuccessStatusCode ? RestResult.Success : RestResult.HttpError, response.StatusCode, null, default);
        }
        catch (Exception e)
        {
            return MakeErrorResponse<object>(e, response?.StatusCode ?? 0);
        }
#pragma warning restore CA1031
    }

    [RequiresUnreferencedCode("JSON serialization/deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization/deserialization requires dynamic code.")]
    public static ValueTask<IRestResponse<T>> PostAsync<T>(
        this HttpClient client,
        string path,
        object parameter,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return client.PostAsync<T>(RestConfig.Default, path, parameter, headers, compress, cancel);
    }

    [RequiresUnreferencedCode("JSON serialization/deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization/deserialization requires dynamic code.")]
    public static async ValueTask<IRestResponse<T>> PostAsync<T>(
        this HttpClient client,
        RestConfig config,
        string path,
        object parameter,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        HttpResponseMessage? response = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);
#pragma warning disable CA2007
            await using var stream = new MemoryStream();
#pragma warning restore CA2007

            ProcessHeaders(request, headers);

            try
            {
                await config.Serializer.SerializeAsync(stream, parameter, cancel).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return new RestResponse<T>(RestResult.SerializeError, 0, e, default);
            }

            stream.Seek(0, SeekOrigin.Begin);
#pragma warning disable CA2000
            var content = (HttpContent)new StreamContent(stream);
#pragma warning restore CA2000
            content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
            if (compress != CompressOption.None)
            {
                content = new CompressedContent(content, compress);
            }

            request.Content = content;

            response = await client.SendAsync(request, cancel).ConfigureAwait(false);

            var isJson = response.Content.Headers.ContentType?.MediaType is not null &&
                         response.Content.Headers.ContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
            try
            {
                var obj = isJson ? await config.Serializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), cancel).ConfigureAwait(false) : default;
                return new RestResponse<T>(response.IsSuccessStatusCode ? RestResult.Success : RestResult.HttpError, response.StatusCode, null, obj);
            }
            catch (Exception e)
            {
                return new RestResponse<T>(RestResult.SerializeError, response.StatusCode, e, default);
            }
        }
        catch (Exception e)
        {
            return MakeErrorResponse<T>(e, response?.StatusCode ?? 0);
        }
#pragma warning restore CA1031
    }

    /// <summary>AOT対応: JsonTypeInfoを使用してシリアライズ・デシリアライズします。</summary>
    public static ValueTask<IRestResponse> PostAsync<TRequest>(
        this HttpClient client,
        string path,
        TRequest parameter,
        JsonTypeInfo<TRequest> requestTypeInfo,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return client.PostAsync(RestConfig.Default, path, parameter, requestTypeInfo, headers, compress, cancel);
    }

    /// <summary>AOT対応: JsonTypeInfoを使用してシリアライズ・デシリアライズします。</summary>
    public static async ValueTask<IRestResponse> PostAsync<TRequest>(
        this HttpClient client,
        RestConfig config,
        string path,
        TRequest parameter,
        JsonTypeInfo<TRequest> requestTypeInfo,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        HttpResponseMessage? response = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);
#pragma warning disable CA2007
            await using var stream = new MemoryStream();
#pragma warning restore CA2007

            ProcessHeaders(request, headers);

            try
            {
                await config.Serializer.SerializeAsync(stream, parameter, requestTypeInfo, cancel).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return new RestResponse<object>(RestResult.SerializeError, 0, e, default);
            }

            stream.Seek(0, SeekOrigin.Begin);
#pragma warning disable CA2000
            var content = (HttpContent)new StreamContent(stream);
#pragma warning restore CA2000
            content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
            if (compress != CompressOption.None)
            {
                content = new CompressedContent(content, compress);
            }

            request.Content = content;

            response = await client.SendAsync(request, cancel).ConfigureAwait(false);
            return new RestResponse<object>(response.IsSuccessStatusCode ? RestResult.Success : RestResult.HttpError, response.StatusCode, null, default);
        }
        catch (Exception e)
        {
            return MakeErrorResponse<object>(e, response?.StatusCode ?? 0);
        }
#pragma warning restore CA1031
    }

    public static ValueTask<IRestResponse<TResponse>> PostAsync<TRequest, TResponse>(
        this HttpClient client,
        string path,
        TRequest parameter,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<TResponse> responseTypeInfo,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return client.PostAsync(RestConfig.Default, path, parameter, requestTypeInfo, responseTypeInfo, headers, compress, cancel);
    }

    public static async ValueTask<IRestResponse<TResponse>> PostAsync<TRequest, TResponse>(
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
        HttpResponseMessage? response = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);
#pragma warning disable CA2007
            await using var stream = new MemoryStream();
#pragma warning restore CA2007

            ProcessHeaders(request, headers);

            try
            {
                await config.Serializer.SerializeAsync(stream, parameter, requestTypeInfo, cancel).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return new RestResponse<TResponse>(RestResult.SerializeError, 0, e, default);
            }

            stream.Seek(0, SeekOrigin.Begin);
#pragma warning disable CA2000
            var content = (HttpContent)new StreamContent(stream);
#pragma warning restore CA2000
            content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
            if (compress != CompressOption.None)
            {
                content = new CompressedContent(content, compress);
            }

            request.Content = content;

            response = await client.SendAsync(request, cancel).ConfigureAwait(false);

            var isJson = response.Content.Headers.ContentType?.MediaType is not null &&
                         response.Content.Headers.ContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
            try
            {
                var obj = isJson ? await config.Serializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), responseTypeInfo, cancel).ConfigureAwait(false) : default;
                return new RestResponse<TResponse>(response.IsSuccessStatusCode ? RestResult.Success : RestResult.HttpError, response.StatusCode, null, obj);
            }
            catch (Exception e)
            {
                return new RestResponse<TResponse>(RestResult.SerializeError, response.StatusCode, e, default);
            }
        }
        catch (Exception e)
        {
            return MakeErrorResponse<TResponse>(e, response?.StatusCode ?? 0);
        }
#pragma warning restore CA1031
    }
}
