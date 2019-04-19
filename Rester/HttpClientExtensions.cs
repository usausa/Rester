namespace Rester
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static partial class HttpClientExtensions
    {
        private static HttpResponse<T> MakeErrorResponse<T>(Exception e, HttpStatusCode statusCode)
        {
            switch (e)
            {
                case HttpRequestException hre:
                    return new HttpResponse<T>(HttpResultType.RequestError, statusCode, hre, default);
                case WebException we:
                    return new HttpResponse<T>(HttpResultType.HttpError, (we.Response as HttpWebResponse)?.StatusCode ?? statusCode, we, default);
                case TaskCanceledException tce:
                    return new HttpResponse<T>(HttpResultType.Cancel, statusCode, tce, default);
            }

            return new HttpResponse<T>(HttpResultType.Unknown, statusCode, e, default);
        }

        private static void ProcessHeaders(HttpRequestMessage request, IDictionary<string, object> headers)
        {
            if (headers == null)
            {
                return;
            }

            foreach (var header in headers)
            {
                switch (header.Value)
                {
                    case IEnumerable<string> ies:
                        request.Headers.Add(header.Key, ies);
                        break;
                    case IEnumerable<object> ie:
                        request.Headers.Add(header.Key, ie.Select(x => x.ToString()));
                        break;
                    default:
                        request.Headers.Add(header.Key, header.Value.ToString());
                        break;
                }
            }
        }
    }
}
