namespace Rester
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public static partial class HttpClientExtensions
    {
        public static Task<IRestResponse> PostAsync(
            this HttpClient client,
            string path,
            object parameter,
            IDictionary<string, object> headers = null,
            bool compress = false,
            CancellationToken cancel = default)
        {
            return PostAsync(client, RestConfig.Default, path, parameter, headers, compress, cancel);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
        public static async Task<IRestResponse> PostAsync(
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
                    return new RestResponse<object>(RestResult.SerializeError, 0, e, default);
                }

                var content = (HttpContent)new StringContent(data, config.Encoding, config.Serializer.ContentType);
                if (compress)
                {
                    content = new CompressedContent(content, config.ContentEncoding);
                }

                request.Content = content;

                response = await client.SendAsync(request, cancel).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new RestResponse<object>(RestResult.HttpError, response.StatusCode, null, default);
                }

                return new RestResponse<object>(RestResult.Success, response.StatusCode, null, default);
            }
            catch (Exception e)
            {
                return MakeErrorResponse<object>(e, response?.StatusCode ?? 0);
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

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2008:DoNotCreateTasksWithoutPassingATaskScheduler", Justification = "Ignore")]
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                var compressedStream = contentEncoding == ContentEncoding.Gzip
                    ? (Stream)new GZipStream(stream, CompressionMode.Compress, true)
                    : new DeflateStream(stream, CompressionMode.Compress, true);
                return content.CopyToAsync(compressedStream, context)
                    .ContinueWith(t => compressedStream.Dispose());
            }
        }
    }
}
