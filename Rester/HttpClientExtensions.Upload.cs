namespace Rester
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Rester.Transfer;

    public static partial class HttpClientExtensions
    {
        public static Task<IRestResponse> UploadAsync(
            this HttpClient client,
            string path,
            Stream stream,
            string name,
            string filename,
            Func<Stream, Stream, Func<Stream, Stream, Task>, Task> filter = null,
            IDictionary<string, object> parameters = null,
            IDictionary<string, object> headers = null,
            Action<long, long> progress = null,
            CancellationToken cancel = default)
        {
            return UploadAsync(client, RestConfig.Default, path, stream, name, filename, filter, parameters, headers, progress, cancel);
        }

        public static Task<IRestResponse> UploadAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            Stream stream,
            string name,
            string filename,
            Func<Stream, Stream, Func<Stream, Stream, Task>, Task> filter = null,
            IDictionary<string, object> parameters = null,
            IDictionary<string, object> headers = null,
            Action<long, long> progress = null,
            CancellationToken cancel = default)
        {
            return UploadAsync(client, config, path, new[] { new UploadEntry(stream, name, filename) { Filter = filter } }, parameters, headers, progress, cancel);
        }

        public static Task<IRestResponse> UploadAsync(
            this HttpClient client,
            string path,
            string name,
            string filename,
            Func<Stream, Stream, Func<Stream, Stream, Task>, Task> filter = null,
            IDictionary<string, object> parameters = null,
            IDictionary<string, object> headers = null,
            Action<long, long> progress = null,
            CancellationToken cancel = default)
        {
            return UploadAsync(client, RestConfig.Default, path, name, filename, filter, parameters, headers, progress, cancel);
        }

        public static async Task<IRestResponse> UploadAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            string name,
            string filename,
            Func<Stream, Stream, Func<Stream, Stream, Task>, Task> filter = null,
            IDictionary<string, object> parameters = null,
            IDictionary<string, object> headers = null,
            Action<long, long> progress = null,
            CancellationToken cancel = default)
        {
            var fi = new FileInfo(filename);
            using (var stream = fi.OpenRead())
            {
                return await UploadAsync(client, config, path, new[] { new UploadEntry(stream, name, fi.Name) { Filter = filter } }, parameters, headers, progress, cancel).ConfigureAwait(false);
            }
        }

        public static Task<IRestResponse> UploadAsync(
            this HttpClient client,
            string path,
            IList<UploadEntry> entries,
            IDictionary<string, object> parameters = null,
            IDictionary<string, object> headers = null,
            Action<long, long> progress = null,
            CancellationToken cancel = default)
        {
            return UploadAsync(client, RestConfig.Default, path, entries, parameters, headers, progress, cancel);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
        public static async Task<IRestResponse> UploadAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            IList<UploadEntry> entries,
            IDictionary<string, object> parameters = null,
            IDictionary<string, object> headers = null,
            Action<long, long> progress = null,
            CancellationToken cancel = default)
        {
            HttpResponseMessage response = null;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, path);
                ProcessHeaders(request, headers);

                using (var multipart = new MultipartFormDataContent())
                {
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            multipart.Add(new StringContent(parameter.Value.ToString()), parameter.Key);
                        }
                    }

                    var progressProxy = default(Action<long>);
                    if (progress != null)
                    {
                        var totalSize = CalcTotalSize(entries);
                        if (totalSize.HasValue)
                        {
                            var totalProcessed = 0L;
                            progressProxy = (processed) =>
                            {
                                totalProcessed += processed;
                                progress(totalProcessed, totalSize.Value);
                            };
                        }
                    }

                    foreach (var upload in entries)
                    {
                        multipart.Add(new UploadStreamContent(upload, config.TransferBufferSize, progressProxy, cancel), upload.Name, upload.FileName);
                    }

                    request.Content = multipart;

                    response = await client.SendAsync(request, cancel).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        return new RestResponse<object>(RestResult.HttpError, response.StatusCode, null, default);
                    }

                    return new RestResponse<object>(RestResult.Success, response.StatusCode, null, default);
                }
            }
            catch (Exception e)
            {
                return MakeErrorResponse<object>(e, response?.StatusCode ?? 0);
            }
        }

        private static long? CalcTotalSize(IList<UploadEntry> entries)
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

            return total;
        }

        private sealed class UploadStreamContent : HttpContent
        {
            private readonly Stream source;

            private readonly Func<Stream, Stream, Func<Stream, Stream, Task>, Task> filter;

            private readonly int bufferSize;

            private readonly Action<long> progress;

            private readonly CancellationToken cancel;

            public UploadStreamContent(UploadEntry entry, int bufferSize, Action<long> progress, CancellationToken cancel)
            {
                source = entry.Stream;
                filter = entry.Filter;
                this.bufferSize = bufferSize;
                this.progress = progress;
                this.cancel = cancel;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    source.Dispose();
                }

                base.Dispose(disposing);
            }

            protected override bool TryComputeLength(out long length)
            {
                if ((filter == null) && source.CanSeek)
                {
                    length = source.Length;
                    return true;
                }

                length = 0;
                return false;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                if (progress == null)
                {
                    if (filter == null)
                    {
                        return source.CopyToAsync(stream, bufferSize, cancel);
                    }

                    return filter(source, stream, (s, d) => s.CopyToAsync(d, bufferSize, cancel));
                }

                if (filter == null)
                {
                    return CopyAsync(source, stream, bufferSize, progress, cancel);
                }

                return filter(source, stream, (s, d) => CopyAsync(s, d, bufferSize, progress, cancel));
            }

            private static async Task CopyAsync(Stream source, Stream destination, int bufferSize, Action<long> progress, CancellationToken cancel)
            {
                var buffer = new byte[bufferSize];
                int read;
                while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancel).ConfigureAwait(false)) > 0)
                {
                    await destination.WriteAsync(buffer, 0, read, cancel).ConfigureAwait(false);
                    progress(read);
                }

                await destination.FlushAsync(cancel).ConfigureAwait(false);
            }
        }
    }
}