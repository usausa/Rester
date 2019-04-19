namespace Rester
{
    using System;
    using System.Net;

    public interface IHttpResponse
    {
        HttpResultType ResultType { get; }

        HttpStatusCode StatusCode { get; }

        Exception InnerException { get; }
    }

    public interface IHttpResponse<out T> : IHttpResponse
    {
        T Content { get; }
    }

    public sealed class HttpResponse<T> : IHttpResponse<T>
    {
        public HttpResultType ResultType { get; }

        public HttpStatusCode StatusCode { get; }

        public Exception InnerException { get; }

        public T Content { get; }

        public HttpResponse(HttpResultType resultType, HttpStatusCode statusCode, Exception innerException, T content)
        {
            ResultType = resultType;
            StatusCode = statusCode;
            InnerException = innerException;
            Content = content;
        }
    }

    public static class HttpResponseExtensions
    {
        public static bool IsSuccess(this IHttpResponse response) => response.ResultType == HttpResultType.Success;
    }
}
