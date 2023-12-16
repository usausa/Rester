namespace Rester;

using System.Net.Http;

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
}
