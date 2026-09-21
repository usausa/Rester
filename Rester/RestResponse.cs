namespace Rester;

using System.Net;
using System.Net.Http.Headers;

public interface IRestResponse
{
    RestResult RestResult { get; }

    HttpStatusCode StatusCode { get; }

    HttpResponseHeaders? Headers { get; }

    Exception? InnerException { get; }
}

public interface IRestResponse<out T> : IRestResponse
{
    T? Content { get; }
}

public sealed class RestResponse<T> : IRestResponse<T>
{
    public RestResult RestResult { get; }

    public HttpStatusCode StatusCode { get; }

    public HttpResponseHeaders? Headers { get; }

    public Exception? InnerException { get; }

    public T? Content { get; }

    public RestResponse(RestResult restResult, HttpStatusCode statusCode, HttpResponseHeaders? headers, Exception? innerException, T? content)
    {
        RestResult = restResult;
        StatusCode = statusCode;
        Headers = headers;
        InnerException = innerException;
        Content = content;
    }
}

public static class HttpResponseExtensions
{
    public static bool IsSuccess(this IRestResponse response) => response.RestResult == RestResult.Success;
}
