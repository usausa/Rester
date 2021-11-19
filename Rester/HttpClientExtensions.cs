namespace Rester;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

public static partial class HttpClientExtensions
{
    private static RestResponse<T> MakeErrorResponse<T>(Exception e, HttpStatusCode statusCode)
    {
        return e switch
        {
            HttpRequestException hre => new RestResponse<T>(RestResult.RequestError, statusCode, hre, default),
            WebException we => new RestResponse<T>(RestResult.HttpError, (we.Response as HttpWebResponse)?.StatusCode ?? statusCode, we, default),
            TaskCanceledException tce => new RestResponse<T>(RestResult.Cancel, statusCode, tce, default),
            _ => new RestResponse<T>(RestResult.Unknown, statusCode, e, default)
        };
    }

    private static void ProcessHeaders(HttpRequestMessage request, IDictionary<string, object>? headers)
    {
        if (headers is null)
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
