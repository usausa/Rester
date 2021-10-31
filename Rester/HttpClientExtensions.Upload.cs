namespace Rester
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Rester.Internal;

    public static partial class HttpClientExtensions
    {
        public static ValueTask<IRestResponse> UploadAsync(
            this HttpClient client,
            string path,
            string filename,
            IDictionary<string, object>? headers = null,
            CompressOption compress = CompressOption.None,
            Action<long, long>? progress = null,
            CancellationToken cancel = default)
        {
            return UploadAsync(client, RestConfig.Default, path, filename, headers, compress, progress, cancel);
        }

        public static async ValueTask<IRestResponse> UploadAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            string filename,
            IDictionary<string, object>? headers = null,
            CompressOption compress = CompressOption.None,
            Action<long, long>? progress = null,
            CancellationToken cancel = default)
        {
            var fi = new FileInfo(filename);
            await using var stream = fi.OpenRead();
            return await UploadAsync(client, config, path, stream, headers, compress, progress, cancel).ConfigureAwait(false);
        }

        public static ValueTask<IRestResponse> UploadAsync(
            this HttpClient client,
            string path,
            Stream stream,
            IDictionary<string, object>? headers = null,
            CompressOption compress = CompressOption.None,
            Action<long, long>? progress = null,
            CancellationToken cancel = default)
        {
            return UploadAsync(client, RestConfig.Default, path, stream, headers, compress, progress, cancel);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
        public static async ValueTask<IRestResponse> UploadAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            Stream stream,
            IDictionary<string, object>? headers = null,
            CompressOption compress = CompressOption.None,
            Action<long, long>? progress = null,
            CancellationToken cancel = default)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, path);

                ProcessHeaders(request, headers);

                var progressProxy = progress is not null ? MakeProgress(stream, progress) : default;

                request.Content = new UploadStreamContent(stream, config.TransferBufferSize, compress, progressProxy, cancel);

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

        private static Action<long>? MakeProgress(Stream stream, Action<long, long> progress)
        {
            if (!stream.CanSeek)
            {
                return null;
            }

            var total = stream.Length;

            var totalProcessed = 0L;
            return processed =>
            {
                totalProcessed += processed;
                progress(totalProcessed, total);
            };
        }
    }
}
