namespace Rester
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    public static partial class HttpClientExtensions
    {
        public static ValueTask<IRestResponse> PostAsync(
            this HttpClient client,
            string path,
            object parameter,
            IDictionary<string, object>? headers = null,
            bool compress = false,
            CancellationToken cancel = default)
        {
            return PostAsync(client, RestConfig.Default, path, parameter, headers, compress, cancel);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
        public static async ValueTask<IRestResponse> PostAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            object parameter,
            IDictionary<string, object>? headers = null,
            bool compress = false,
            CancellationToken cancel = default)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, path);
                await using var stream = new MemoryStream();

                ProcessHeaders(request, headers);

                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, cancel).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    return new RestResponse<object>(RestResult.SerializeError, 0, e, default);
                }

                stream.Seek(0, SeekOrigin.Begin);
                var content = (HttpContent)new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
                if (compress)
                {
                    content = new CompressedContent(content, config.ContentEncoding);
                }

                request.Content = content;

                response = await client.SendAsync(request, cancel).ConfigureAwait(false);
                return new RestResponse<object>(response.IsSuccessStatusCode ? RestResult.Success : RestResult.HttpError, response.StatusCode, null, default);
            }
            catch (Exception e)
            {
                return MakeErrorResponse<object>(e, response?.StatusCode ?? 0);
            }
        }

        public static ValueTask<IRestResponse<T>> PostAsync<T>(
            this HttpClient client,
            string path,
            object parameter,
            IDictionary<string, object>? headers = null,
            bool compress = false,
            CancellationToken cancel = default)
        {
            return PostAsync<T>(client, RestConfig.Default, path, parameter, headers, compress, cancel);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
        public static async ValueTask<IRestResponse<T>> PostAsync<T>(
            this HttpClient client,
            RestConfig config,
            string path,
            object parameter,
            IDictionary<string, object>? headers = null,
            bool compress = false,
            CancellationToken cancel = default)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, path);
                await using var stream = new MemoryStream();

                ProcessHeaders(request, headers);

                try
                {
                    await config.Serializer.SerializeAsync(stream, parameter, cancel).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    return new RestResponse<T>(RestResult.SerializeError, 0, e, default);
                }

                stream.Seek(0, SeekOrigin.Begin);
                var content = (HttpContent)new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(config.Serializer.ContentType);
                if (compress)
                {
                    content = new CompressedContent(content, config.ContentEncoding);
                }

                request.Content = content;

                response = await client.SendAsync(request, cancel).ConfigureAwait(false);

                var isJson = response.Content.Headers.ContentType?.MediaType is not null &&
                             response.Content.Headers.ContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase);
                try
                {
#if NET5_0
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

        private sealed class CompressedContent : HttpContent
        {
            private readonly HttpContent content;

            private readonly ContentEncoding contentEncoding;

            public CompressedContent(HttpContent content, ContentEncoding contentEncoding)
            {
                this.content = content;
                this.contentEncoding = contentEncoding;

                foreach (var header in content.Headers)
                {
                    Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                Headers.ContentEncoding.Add(contentEncoding.ToString().ToLowerInvariant());
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    content.Dispose();
                }

                base.Dispose(disposing);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Factory")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2008:DoNotCreateTasksWithoutPassingATaskScheduler", Justification = "Ignore")]
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                var compressedStream = contentEncoding == ContentEncoding.Gzip
                    ? (Stream)new GZipStream(stream, CompressionMode.Compress, true)
                    : new DeflateStream(stream, CompressionMode.Compress, true);
                return content.CopyToAsync(compressedStream, context)
                    .ContinueWith(_ => compressedStream.Dispose());
            }
        }
    }
}
