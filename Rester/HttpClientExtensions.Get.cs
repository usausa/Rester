namespace Rester;

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json.Serialization.Metadata;

public static partial class HttpClientExtensions
{
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization requires dynamic code.")]
    public static ValueTask<IRestResponse<T>> GetAsync<T>(
        this HttpClient client,
        string path,
        IDictionary<string, object>? headers = null,
        CancellationToken cancel = default)
    {
        return client.GetAsync<T>(RestConfig.Default, path, headers, cancel);
    }

    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization requires dynamic code.")]
    public static async ValueTask<IRestResponse<T>> GetAsync<T>(
        this HttpClient client,
        RestConfig config,
        string path,
        IDictionary<string, object>? headers = null,
        CancellationToken cancel = default)
    {
        HttpResponseMessage? response = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);

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
            return MakeErrorResponse<T>(e, response?.StatusCode ?? 0, cancel);
        }
        finally
        {
            response?.Dispose();
        }
#pragma warning restore CA1031
    }

    public static ValueTask<IRestResponse<T>> GetAsync<T>(
        this HttpClient client,
        string path,
        JsonTypeInfo<T> typeInfo,
        IDictionary<string, object>? headers = null,
        CancellationToken cancel = default)
    {
        return client.GetAsync(RestConfig.Default, path, typeInfo, headers, cancel);
    }

    public static async ValueTask<IRestResponse<T>> GetAsync<T>(
        this HttpClient client,
        RestConfig config,
        string path,
        JsonTypeInfo<T> typeInfo,
        IDictionary<string, object>? headers = null,
        CancellationToken cancel = default)
    {
        HttpResponseMessage? response = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);

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
                var obj = isJson ? await config.Serializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), typeInfo, cancel).ConfigureAwait(false) : default;
                return new RestResponse<T>(RestResult.Success, response.StatusCode, null, obj);
            }
            catch (Exception e) when ((e is not OperationCanceledException) && !cancel.IsCancellationRequested)
            {
                return new RestResponse<T>(RestResult.SerializeError, response.StatusCode, e, default);
            }
        }
        catch (Exception e)
        {
            return MakeErrorResponse<T>(e, response?.StatusCode ?? 0, cancel);
        }
        finally
        {
            response?.Dispose();
        }
#pragma warning restore CA1031
    }
}
