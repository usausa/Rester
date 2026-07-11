namespace Rester;

using System.Net;
using System.Net.Http;

public static partial class HttpClientExtensions
{
    private static RestResponse<T> MakeErrorResponse<T>(Exception e, HttpStatusCode statusCode, CancellationToken cancel)
    {
        return e switch
        {
            TaskCanceledException { InnerException: TimeoutException } tce => new RestResponse<T>(RestResult.Timeout, statusCode, tce, default),
            _ when cancel.IsCancellationRequested => new RestResponse<T>(RestResult.Cancel, statusCode, e, default),
            HttpRequestException hre => new RestResponse<T>(RestResult.RequestError, statusCode, hre, default),
            WebException we => new RestResponse<T>(RestResult.HttpError, (we.Response as HttpWebResponse)?.StatusCode ?? statusCode, we, default),
            TaskCanceledException tce => new RestResponse<T>(RestResult.Cancel, statusCode, tce, default),
            OperationCanceledException oce => new RestResponse<T>(RestResult.Cancel, statusCode, oce, default),
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
                case null:
                    throw new ArgumentException($"Header value is null. name=[{header.Key}]", nameof(headers));
                case IEnumerable<string> ies:
                    AddHeader(request, header.Key, ToValues(ies));
                    break;
                case IEnumerable<object> ie:
                    AddHeader(request, header.Key, ToValues(ie.Select(static x => x.ToString())));
                    break;
                default:
                    AddHeader(request, header.Key, header.Value.ToString() ?? string.Empty);
                    break;
            }
        }
    }

    private static List<string> ToValues(IEnumerable<string?> values)
    {
        var list = new List<string>();
        foreach (var value in values)
        {
            list.Add(value ?? string.Empty);
        }

        return list;
    }

    private static void AddHeader(HttpRequestMessage request, string name, string value)
    {
        if (!request.Headers.TryAddWithoutValidation(name, value) &&
            ((request.Content is null) || !request.Content.Headers.TryAddWithoutValidation(name, value)))
        {
            throw new ArgumentException($"Invalid header. name=[{name}]");
        }
    }

    private static void AddHeader(HttpRequestMessage request, string name, List<string> values)
    {
        if (!request.Headers.TryAddWithoutValidation(name, values) &&
            ((request.Content is null) || !request.Content.Headers.TryAddWithoutValidation(name, values)))
        {
            throw new ArgumentException($"Invalid header. name=[{name}]");
        }
    }
}
