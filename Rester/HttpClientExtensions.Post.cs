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
        SerializeContent? serializeContent = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);

            HttpContent content;
#pragma warning disable CA2000
            if (config.PostContentStreaming)
            {
                serializeContent = new SerializeContent((stream, token) => config.Serializer.SerializeAsync(stream, parameter, token), cancel);
                content = serializeContent;
            }
            else
            {
                var stream = new MemoryStream();
                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, cancel).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                    return new RestResponse<object>(RestResult.SerializeError, 0, e, default);
                }

                stream.Seek(0, SeekOrigin.Begin);
                content = new StreamContent(stream);
            }
#pragma warning restore CA2000

            content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
            if (compress != CompressOption.None)
            {
                content = new CompressedContent(content, compress);
            }

            request.Content = content;

            ProcessHeaders(request, headers);

            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
            return new RestResponse<object>(response.IsSuccessStatusCode ? RestResult.Success : RestResult.HttpError, response.StatusCode, null, default);
        }
        catch (Exception e)
        {
            if ((serializeContent?.SerializeError is { } serializeError) && (e is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<object>(RestResult.SerializeError, 0, serializeError, default);
            }

            return MakeErrorResponse<object>(e, response?.StatusCode ?? 0, cancel);
        }
        finally
        {
            response?.Dispose();
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
        SerializeContent? serializeContent = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);

            HttpContent content;
#pragma warning disable CA2000
            if (config.PostContentStreaming)
            {
                serializeContent = new SerializeContent((stream, token) => config.Serializer.SerializeAsync(stream, parameter, token), cancel);
                content = serializeContent;
            }
            else
            {
                var stream = new MemoryStream();
                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, cancel).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                    return new RestResponse<T>(RestResult.SerializeError, 0, e, default);
                }

                stream.Seek(0, SeekOrigin.Begin);
                content = new StreamContent(stream);
            }
#pragma warning restore CA2000

            content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
            if (compress != CompressOption.None)
            {
                content = new CompressedContent(content, compress);
            }

            request.Content = content;

            ProcessHeaders(request, headers);

            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new RestResponse<T>(RestResult.HttpError, response.StatusCode, null, default);
            }

            var isJson = response.Content.Headers.ContentType?.MediaType is not null &&
                         response.Content.Headers.ContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
            try
            {
                var obj = isJson ? await config.Serializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), cancel).ConfigureAwait(false) : default;
                return new RestResponse<T>(RestResult.Success, response.StatusCode, null, obj);
            }
            catch (Exception e) when ((e is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<T>(RestResult.SerializeError, response.StatusCode, e, default);
            }
        }
        catch (Exception e)
        {
            if ((serializeContent?.SerializeError is { } serializeError) && (e is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<T>(RestResult.SerializeError, 0, serializeError, default);
            }

            return MakeErrorResponse<T>(e, response?.StatusCode ?? 0, cancel);
        }
        finally
        {
            response?.Dispose();
        }
#pragma warning restore CA1031
    }

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
        SerializeContent? serializeContent = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);

            HttpContent content;
#pragma warning disable CA2000
            if (config.PostContentStreaming)
            {
                serializeContent = new SerializeContent((stream, token) => config.Serializer.SerializeAsync(stream, parameter, requestTypeInfo, token), cancel);
                content = serializeContent;
            }
            else
            {
                var stream = new MemoryStream();
                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, requestTypeInfo, cancel).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                    return new RestResponse<object>(RestResult.SerializeError, 0, e, default);
                }

                stream.Seek(0, SeekOrigin.Begin);
                content = new StreamContent(stream);
            }
#pragma warning restore CA2000

            content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
            if (compress != CompressOption.None)
            {
                content = new CompressedContent(content, compress);
            }

            request.Content = content;

            ProcessHeaders(request, headers);

            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
            return new RestResponse<object>(response.IsSuccessStatusCode ? RestResult.Success : RestResult.HttpError, response.StatusCode, null, default);
        }
        catch (Exception e)
        {
            if ((serializeContent?.SerializeError is { } serializeError) && (e is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<object>(RestResult.SerializeError, 0, serializeError, default);
            }

            return MakeErrorResponse<object>(e, response?.StatusCode ?? 0, cancel);
        }
        finally
        {
            response?.Dispose();
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
        SerializeContent? serializeContent = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);

            HttpContent content;
#pragma warning disable CA2000
            if (config.PostContentStreaming)
            {
                serializeContent = new SerializeContent((stream, token) => config.Serializer.SerializeAsync(stream, parameter, requestTypeInfo, token), cancel);
                content = serializeContent;
            }
            else
            {
                var stream = new MemoryStream();
                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, requestTypeInfo, cancel).ConfigureAwait(false);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                    return new RestResponse<TResponse>(RestResult.SerializeError, 0, e, default);
                }

                stream.Seek(0, SeekOrigin.Begin);
                content = new StreamContent(stream);
            }
#pragma warning restore CA2000

            content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
            if (compress != CompressOption.None)
            {
                content = new CompressedContent(content, compress);
            }

            request.Content = content;

            ProcessHeaders(request, headers);

            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new RestResponse<TResponse>(RestResult.HttpError, response.StatusCode, null, default);
            }

            var isJson = response.Content.Headers.ContentType?.MediaType is not null &&
                         response.Content.Headers.ContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
            try
            {
                var obj = isJson ? await config.Serializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), responseTypeInfo, cancel).ConfigureAwait(false) : default;
                return new RestResponse<TResponse>(RestResult.Success, response.StatusCode, null, obj);
            }
            catch (Exception e) when ((e is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<TResponse>(RestResult.SerializeError, response.StatusCode, e, default);
            }
        }
        catch (Exception e)
        {
            if ((serializeContent?.SerializeError is { } serializeError) && (e is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<TResponse>(RestResult.SerializeError, 0, serializeError, default);
            }

            return MakeErrorResponse<TResponse>(e, response?.StatusCode ?? 0, cancel);
        }
        finally
        {
            response?.Dispose();
        }
#pragma warning restore CA1031
    }
}
