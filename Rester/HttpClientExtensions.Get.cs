namespace Rester;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public static partial class HttpClientExtensions
{
    public static ValueTask<IRestResponse<T>> GetAsync<T>(
        this HttpClient client,
        string path,
        IDictionary<string, object>? headers = null,
        CancellationToken cancel = default)
    {
        return GetAsync<T>(client, RestConfig.Default, path, headers, cancel);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
    public static async ValueTask<IRestResponse<T>> GetAsync<T>(
        this HttpClient client,
        RestConfig config,
        string path,
        IDictionary<string, object>? headers = null,
        CancellationToken cancel = default)
    {
        HttpResponseMessage? response = null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);

            ProcessHeaders(request, headers);

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
