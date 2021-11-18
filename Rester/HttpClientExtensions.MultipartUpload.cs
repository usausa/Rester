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
        public static ValueTask<IRestResponse> MultipartUploadAsync(
            this HttpClient client,
            string path,
            Stream stream,
            string name,
            string filename,
            IDictionary<string, object>? parameters = null,
            IDictionary<string, object>? headers = null,
            CompressOption compress = CompressOption.None,
            Action<long, long>? progress = null,
            CancellationToken cancel = default)
        {
            return MultipartUploadAsync(client, RestConfig.Default, path, stream, name, filename, parameters, headers, compress, progress, cancel);
        }

        public static ValueTask<IRestResponse> MultipartUploadAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            Stream stream,
            string name,
            string filename,
            IDictionary<string, object>? parameters = null,
            IDictionary<string, object>? headers = null,
            CompressOption compress = CompressOption.None,
            Action<long, long>? progress = null,
            CancellationToken cancel = default)
        {
            return MultipartUploadAsync(client, config, path, new[] { new MultipartUploadEntry(stream, name, filename, compress) }, parameters, headers, progress, cancel);
        }

        public static ValueTask<IRestResponse> MultipartUploadAsync(
            this HttpClient client,
            string path,
            string name,
            string filename,
            CompressOption compress = CompressOption.None,
            IDictionary<string, object>? parameters = null,
            IDictionary<string, object>? headers = null,
            Action<long, long>? progress = null,
            CancellationToken cancel = default)
        {
            return MultipartUploadAsync(client, RestConfig.Default, path, name, filename, parameters, headers, compress, progress, cancel);
        }

        public static async ValueTask<IRestResponse> MultipartUploadAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            string name,
            string filename,
            IDictionary<string, object>? parameters = null,
            IDictionary<string, object>? headers = null,
            CompressOption compress = CompressOption.None,
            Action<long, long>? progress = null,
            CancellationToken cancel = default)
        {
            var fi = new FileInfo(filename);
#pragma warning disable CA2007
            await using var stream = fi.OpenRead();
#pragma warning restore CA2007
            return await MultipartUploadAsync(client, config, path, new[] { new MultipartUploadEntry(stream, name, fi.Name, compress) }, parameters, headers, progress, cancel).ConfigureAwait(false);
        }

        public static ValueTask<IRestResponse> MultipartUploadAsync(
            this HttpClient client,
            string path,
            IList<MultipartUploadEntry> entries,
            IDictionary<string, object>? parameters = null,
            IDictionary<string, object>? headers = null,
            Action<long, long>? progress = null,
            CancellationToken cancel = default)
        {
            return MultipartUploadAsync(client, RestConfig.Default, path, entries, parameters, headers, progress, cancel);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
        public static async ValueTask<IRestResponse> MultipartUploadAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            IList<MultipartUploadEntry> entries,
            IDictionary<string, object>? parameters = null,
            IDictionary<string, object>? headers = null,
            Action<long, long>? progress = null,
            CancellationToken cancel = default)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, path);
                using var multipart = new MultipartFormDataContent();

                ProcessHeaders(request, headers);

                if (parameters is not null)
                {
                    foreach (var parameter in parameters)
                    {
#pragma warning disable CA2000
                        multipart.Add(new StringContent(parameter.Value.ToString() ?? string.Empty), parameter.Key);
#pragma warning restore CA2000
                    }
                }

                var progressProxy = progress is not null ? MakeProgress(entries, progress) : default;

                foreach (var upload in entries)
                {
#pragma warning disable CA2000
                    multipart.Add(new UploadStreamContent(upload.Stream, config.TransferBufferSize, upload.Compress, progressProxy, cancel), upload.Name, upload.FileName);
#pragma warning restore CA2000
                }

                request.Content = multipart;

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

        private static Action<long>? MakeProgress(IEnumerable<MultipartUploadEntry> entries, Action<long, long> progress)
        {
            var total = 0L;
            foreach (var upload in entries)
            {
                if (!upload.Stream.CanSeek)
                {
                    return null;
                }

                total += upload.Stream.Length;
            }

            var totalProcessed = 0L;
            return processed =>
            {
                totalProcessed += processed;
                progress(totalProcessed, total);
            };
        }
    }
}
