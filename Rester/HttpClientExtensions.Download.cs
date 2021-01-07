namespace Rester
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Rester.Transfer;

    public static partial class HttpClientExtensions
    {
        public static ValueTask<IRestResponse> DownloadAsync(
            this HttpClient client,
            string path,
            string filename,
            IDictionary<string, object> headers = null,
            Action<long, long> progress = null,
            CancellationToken cancel = default)
        {
            return DownloadAsync(client, RestConfig.Default, path, filename, headers, progress, cancel);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Ignore")]
        public static async ValueTask<IRestResponse> DownloadAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            string filename,
            IDictionary<string, object> headers = null,
            Action<long, long> progress = null,
            CancellationToken cancel = default)
        {
            var delete = true;
            try
            {
                await using (var stream = new FileStream(filename, FileMode.Create))
                {
                    var result = await DownloadAsync(client, config, path, stream, headers, progress, cancel).ConfigureAwait(false);
                    if (result.IsSuccess())
                    {
                        delete = false;
                    }

                    return result;
                }
            }
            finally
            {
                if (delete)
                {
                    File.Delete(filename);
                }
            }
        }

        public static ValueTask<IRestResponse> DownloadAsync(
            this HttpClient client,
            string path,
            Stream stream,
            IDictionary<string, object> headers = null,
            Action<long, long> progress = null,
            CancellationToken cancel = default)
        {
            return DownloadAsync(client, RestConfig.Default, path, stream, headers, progress, cancel);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Ignore")]
        public static async ValueTask<IRestResponse> DownloadAsync(
            this HttpClient client,
            RestConfig config,
            string path,
            Stream stream,
            IDictionary<string, object> headers = null,
            Action<long, long> progress = null,
            CancellationToken cancel = default)
        {
            HttpResponseMessage response = null;
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, path))
                {
                    ProcessHeaders(request, headers);

                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        return new RestResponse<object>(RestResult.HttpError, response.StatusCode, null, default);
                    }

#if NET5_0
                    await using (var input = await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false))
#else
                    await using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
#endif
                    {
                        if (progress != null)
                        {
                            var totalSize = response.Content.Headers.ContentLength ??
                                            config.LengthResolver?.Invoke(new LengthResolveContext(response));
                            if (totalSize.HasValue)
                            {
                                var buffer = new byte[config.TransferBufferSize];
                                var totalProcessed = 0L;
                                int read;
                                while ((read = await input.ReadAsync(buffer, cancel).ConfigureAwait(false)) > 0)
                                {
                                    await stream.WriteAsync(buffer.AsMemory(0, read), cancel).ConfigureAwait(false);

                                    totalProcessed += read;
                                    progress(totalProcessed, totalSize.Value);
                                }
                            }
                            else
                            {
                                await input.CopyToAsync(stream, config.TransferBufferSize, cancel).ConfigureAwait(false);
                                await stream.FlushAsync(cancel).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await input.CopyToAsync(stream, config.TransferBufferSize, cancel).ConfigureAwait(false);
                            await stream.FlushAsync(cancel).ConfigureAwait(false);
                        }
                    }

                    return new RestResponse<object>(RestResult.Success, response.StatusCode, null, default);
                }
            }
            catch (Exception e)
            {
                return MakeErrorResponse<object>(e, response?.StatusCode ?? 0);
            }
        }
    }
}
