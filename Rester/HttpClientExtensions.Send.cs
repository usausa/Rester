namespace Rester;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization.Metadata;

using Rester.Internal;

public static partial class HttpClientExtensions
{
    //--------------------------------------------------------------------------------
    // Send without content
    //--------------------------------------------------------------------------------

    public static async ValueTask<IRestResponse> SendAsync(
        this HttpClient client,
        HttpMethod method,
        string path,
        IDictionary<string, object>? headers = null,
        CancellationToken cancel = default)
    {
        HttpResponseMessage? response = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(method, path);

            ProcessHeaders(request, headers);

            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
            return new RestResponse<object>(response.IsSuccessStatusCode ? RestResult.Success : RestResult.HttpError, response.StatusCode, response.Headers, null, default);
        }
        catch (Exception ex)
        {
            return MakeErrorResponse<object>(ex, response, cancel);
        }
        finally
        {
            response?.Dispose();
        }
#pragma warning restore CA1031
    }

    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization requires dynamic code.")]
    private static async ValueTask<IRestResponse<T>> SendCoreAsync<T>(
        HttpClient client,
        RestConfig config,
        HttpMethod method,
        string path,
        IDictionary<string, object>? headers,
        CancellationToken cancel)
    {
        HttpResponseMessage? response = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(method, path);

            ProcessHeaders(request, headers);

            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new RestResponse<T>(RestResult.HttpError, response.StatusCode, response.Headers, null, default);
            }

            var isJson = (response.Content.Headers.ContentType?.MediaType is not null) &&
                         response.Content.Headers.ContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
            try
            {
                var obj = isJson ? await config.Serializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), cancel).ConfigureAwait(false) : default;
                return new RestResponse<T>(RestResult.Success, response.StatusCode, response.Headers, null, obj);
            }
            catch (Exception ex) when ((ex is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<T>(RestResult.SerializeError, response.StatusCode, response.Headers, ex, default);
            }
        }
        catch (Exception ex)
        {
            return MakeErrorResponse<T>(ex, response, cancel);
        }
        finally
        {
            response?.Dispose();
        }
#pragma warning restore CA1031
    }

    private static async ValueTask<IRestResponse<T>> SendCoreAsync<T>(
        HttpClient client,
        RestConfig config,
        HttpMethod method,
        string path,
        JsonTypeInfo<T> typeInfo,
        IDictionary<string, object>? headers,
        CancellationToken cancel)
    {
        HttpResponseMessage? response = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(method, path);

            ProcessHeaders(request, headers);

            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new RestResponse<T>(RestResult.HttpError, response.StatusCode, response.Headers, null, default);
            }

            var isJson = (response.Content.Headers.ContentType?.MediaType is not null) &&
                         response.Content.Headers.ContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
            try
            {
                var obj = isJson ? await config.Serializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), typeInfo, cancel).ConfigureAwait(false) : default;
                return new RestResponse<T>(RestResult.Success, response.StatusCode, response.Headers, null, obj);
            }
            catch (Exception ex) when ((ex is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<T>(RestResult.SerializeError, response.StatusCode, response.Headers, ex, default);
            }
        }
        catch (Exception ex)
        {
            return MakeErrorResponse<T>(ex, response, cancel);
        }
        finally
        {
            response?.Dispose();
        }
#pragma warning restore CA1031
    }

    //--------------------------------------------------------------------------------
    // Send with content
    //--------------------------------------------------------------------------------

    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization requires dynamic code.")]
    private static async ValueTask<IRestResponse> SendContentCoreAsync(
        HttpClient client,
        RestConfig config,
        HttpMethod method,
        string path,
        object parameter,
        IDictionary<string, object>? headers,
        CompressOption compress,
        CancellationToken cancel)
    {
        HttpResponseMessage? response = null;
        SerializeContent? serializeContent = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(method, path);

            HttpContent content;
#pragma warning disable CA2000
            if (config.PostContentStreaming)
            {
                serializeContent = new SerializeContent((stream, token) => config.Serializer.SerializeAsync(stream, parameter, token), cancel);
                content = serializeContent;
            }
            else
            {
                var stream = new PooledBufferStream();
                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, cancel).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                    if (ex is OperationCanceledException)
                    {
                        throw;
                    }

                    return new RestResponse<object>(RestResult.SerializeError, 0, null, ex, default);
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
            return new RestResponse<object>(response.IsSuccessStatusCode ? RestResult.Success : RestResult.HttpError, response.StatusCode, response.Headers, null, default);
        }
        catch (Exception ex)
        {
            if ((serializeContent?.SerializeError is { } serializeError) && (ex is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<object>(RestResult.SerializeError, 0, null, serializeError, default);
            }

            return MakeErrorResponse<object>(ex, response, cancel);
        }
        finally
        {
            response?.Dispose();
        }
#pragma warning restore CA1031
    }

    [RequiresUnreferencedCode("JSON serialization/deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization/deserialization requires dynamic code.")]
    private static async ValueTask<IRestResponse<T>> SendContentCoreAsync<T>(
        HttpClient client,
        RestConfig config,
        HttpMethod method,
        string path,
        object parameter,
        IDictionary<string, object>? headers,
        CompressOption compress,
        CancellationToken cancel)
    {
        HttpResponseMessage? response = null;
        SerializeContent? serializeContent = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(method, path);

            HttpContent content;
#pragma warning disable CA2000
            if (config.PostContentStreaming)
            {
                serializeContent = new SerializeContent((stream, token) => config.Serializer.SerializeAsync(stream, parameter, token), cancel);
                content = serializeContent;
            }
            else
            {
                var stream = new PooledBufferStream();
                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, cancel).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                    if (ex is OperationCanceledException)
                    {
                        throw;
                    }

                    return new RestResponse<T>(RestResult.SerializeError, 0, null, ex, default);
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
                return new RestResponse<T>(RestResult.HttpError, response.StatusCode, response.Headers, null, default);
            }

            var isJson = (response.Content.Headers.ContentType?.MediaType is not null) &&
                         response.Content.Headers.ContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
            try
            {
                var obj = isJson ? await config.Serializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), cancel).ConfigureAwait(false) : default;
                return new RestResponse<T>(RestResult.Success, response.StatusCode, response.Headers, null, obj);
            }
            catch (Exception ex) when ((ex is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<T>(RestResult.SerializeError, response.StatusCode, response.Headers, ex, default);
            }
        }
        catch (Exception ex)
        {
            if ((serializeContent?.SerializeError is { } serializeError) && (ex is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<T>(RestResult.SerializeError, 0, null, serializeError, default);
            }

            return MakeErrorResponse<T>(ex, response, cancel);
        }
        finally
        {
            response?.Dispose();
        }
#pragma warning restore CA1031
    }

    private static async ValueTask<IRestResponse> SendContentCoreAsync<TRequest>(
        HttpClient client,
        RestConfig config,
        HttpMethod method,
        string path,
        TRequest parameter,
        JsonTypeInfo<TRequest> requestTypeInfo,
        IDictionary<string, object>? headers,
        CompressOption compress,
        CancellationToken cancel)
    {
        HttpResponseMessage? response = null;
        SerializeContent? serializeContent = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(method, path);

            HttpContent content;
#pragma warning disable CA2000
            if (config.PostContentStreaming)
            {
                serializeContent = new SerializeContent((stream, token) => config.Serializer.SerializeAsync(stream, parameter, requestTypeInfo, token), cancel);
                content = serializeContent;
            }
            else
            {
                var stream = new PooledBufferStream();
                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, requestTypeInfo, cancel).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                    if (ex is OperationCanceledException)
                    {
                        throw;
                    }

                    return new RestResponse<object>(RestResult.SerializeError, 0, null, ex, default);
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
            return new RestResponse<object>(response.IsSuccessStatusCode ? RestResult.Success : RestResult.HttpError, response.StatusCode, response.Headers, null, default);
        }
        catch (Exception ex)
        {
            if ((serializeContent?.SerializeError is { } serializeError) && (ex is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<object>(RestResult.SerializeError, 0, null, serializeError, default);
            }

            return MakeErrorResponse<object>(ex, response, cancel);
        }
        finally
        {
            response?.Dispose();
        }
#pragma warning restore CA1031
    }

    private static async ValueTask<IRestResponse<TResponse>> SendContentCoreAsync<TRequest, TResponse>(
        HttpClient client,
        RestConfig config,
        HttpMethod method,
        string path,
        TRequest parameter,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<TResponse> responseTypeInfo,
        IDictionary<string, object>? headers,
        CompressOption compress,
        CancellationToken cancel)
    {
        HttpResponseMessage? response = null;
        SerializeContent? serializeContent = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(method, path);

            HttpContent content;
#pragma warning disable CA2000
            if (config.PostContentStreaming)
            {
                serializeContent = new SerializeContent((stream, token) => config.Serializer.SerializeAsync(stream, parameter, requestTypeInfo, token), cancel);
                content = serializeContent;
            }
            else
            {
                var stream = new PooledBufferStream();
                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, requestTypeInfo, cancel).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await stream.DisposeAsync().ConfigureAwait(false);
                    if (ex is OperationCanceledException)
                    {
                        throw;
                    }

                    return new RestResponse<TResponse>(RestResult.SerializeError, 0, null, ex, default);
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
                return new RestResponse<TResponse>(RestResult.HttpError, response.StatusCode, response.Headers, null, default);
            }

            var isJson = (response.Content.Headers.ContentType?.MediaType is not null) &&
                         response.Content.Headers.ContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
            try
            {
                var obj = isJson ? await config.Serializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), responseTypeInfo, cancel).ConfigureAwait(false) : default;
                return new RestResponse<TResponse>(RestResult.Success, response.StatusCode, response.Headers, null, obj);
            }
            catch (Exception ex) when ((ex is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<TResponse>(RestResult.SerializeError, response.StatusCode, response.Headers, ex, default);
            }
        }
        catch (Exception ex)
        {
            if ((serializeContent?.SerializeError is { } serializeError) && (ex is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<TResponse>(RestResult.SerializeError, 0, null, serializeError, default);
            }

            return MakeErrorResponse<TResponse>(ex, response, cancel);
        }
        finally
        {
            response?.Dispose();
        }
#pragma warning restore CA1031
    }
}
