namespace Rester
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Rester.Internal;

    public static partial class HttpClientExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
        public static async Task<IHttpResponse> PostAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            object parameter,
            IDictionary<string, object> headers = null,
            bool compress = false,
            CancellationToken cancel = default)
        {
            HttpResponseMessage response = null;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, path);
                ProcessHeaders(request, headers);

                string data;
                try
                {
                    data = config.Serializer.Serialize(parameter);
                }
                catch (Exception e)
                {
                    return new HttpResponse<object>(HttpResultType.SerializeError, 0, e, default);
                }

                var content = (HttpContent)new StringContent(data, config.Encoding, config.Serializer.ContentType);
                if (compress)
                {
                    content = new CompressedContent(content, config.PostEncodingType);
                }

                request.Content = content;

                response = await client.SendAsync(request, cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new HttpResponse<object>(HttpResultType.HttpError, response.StatusCode, null, default);
                }

                return new HttpResponse<object>(HttpResultType.Success, response.StatusCode, null, default);
            }
            catch (Exception e)
            {
                return MakeErrorResponse<object>(e, response?.StatusCode ?? 0);
            }
        }

        public static Task<IHttpResponse> PostAsync(
            this HttpClient client,
            string path,
            object parameter,
            IDictionary<string, object> headers = null,
            bool compress = false,
            CancellationToken cancel = default)
        {
            return PostAsync(client, RestConfig.Default, path, parameter, headers, compress, cancel);
        }
    }
}
