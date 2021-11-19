namespace Rester;

using System;
using System.Net;

public interface IRestResponse
{
    RestResult RestResult { get; }

    HttpStatusCode StatusCode { get; }

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

    public Exception? InnerException { get; }

    public T? Content { get; }

    public RestResponse(RestResult restResult, HttpStatusCode statusCode, Exception? innerException, T? content)
    {
        RestResult = restResult;
        StatusCode = statusCode;
        InnerException = innerException;
        Content = content;
    }
}

public static class HttpResponseExtensions
{
    public static bool IsSuccess(this IRestResponse response) => response.RestResult == RestResult.Success;
}
