namespace Rester
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public static partial class HttpClientExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
        public static async Task<IHttpResponse<T>> GetAsync<T>(
            this HttpClient client,
            RestConfig config,
            string path,
            IDictionary<string, object> headers = null,
            CancellationToken cancel = default)
        {
            HttpResponseMessage response = null;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, path);
                ProcessHeaders(request, headers);

                response = await client.SendAsync(request, cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new HttpResponse<T>(HttpResultType.HttpError, response.StatusCode, null, default);
                }

                var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    var obj = config.Serializer.Deserialize<T>(data);
                    return new HttpResponse<T>(HttpResultType.Success, response.StatusCode, null, obj);
                }
                catch (Exception e)
                {
                    return new HttpResponse<T>(HttpResultType.SerializeError, response.StatusCode, e, default);
                }
            }
            catch (Exception e)
            {
                return MakeErrorResponse<T>(e, response?.StatusCode ?? 0);
            }
        }

        public static Task<IHttpResponse<T>> GetAsync<T>(
            this HttpClient client,
            string path,
            IDictionary<string, object> headers = null,
            CancellationToken cancel = default)
        {
            return GetAsync<T>(client, RestConfig.Default, path, headers, cancel);
        }
    }
}
