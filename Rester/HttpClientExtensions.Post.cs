namespace Rester;

using System.Net.Http;
using System.Net.Http.Headers;

using Rester.Internal;

public static partial class HttpClientExtensions
{
    public static ValueTask<IRestResponse> PostAsync(
        this HttpClient client,
        string path,
        object parameter,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return PostAsync(client, RestConfig.Default, path, parameter, headers, compress, cancel);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
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
    }

    public static ValueTask<IRestResponse<T>> PostAsync<T>(
        this HttpClient client,
        string path,
        object parameter,
        IDictionary<string, object>? headers = null,
        CompressOption compress = CompressOption.None,
        CancellationToken cancel = default)
    {
        return PostAsync<T>(client, RestConfig.Default, path, parameter, headers, compress, cancel);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
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
#if NET5_0_OR_GREATER
                var obj = isJson ? await config.Serializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false), cancel).ConfigureAwait(false) : default;
#else
                var obj = isJson ? await config.Serializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), cancel).ConfigureAwait(false) : default;
#endif
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
    }
}
