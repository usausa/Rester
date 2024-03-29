namespace Rester;

using System.Net.Http;
using System.Net.Http.Headers;

using Rester.Internal;

public static partial class HttpClientExtensions
{
    public static ValueTask<IRestResponse> UploadAsync(
        this HttpClient client,
        string path,
        string filename,
        IDictionary<string, object>? headers = null,
        string? contentType = null,
        CompressOption compress = CompressOption.None,
        Action<long, long>? progress = null,
        CancellationToken cancel = default)
    {
        return UploadAsync(client, RestConfig.Default, path, filename, headers, contentType, compress, progress, cancel);
    }

    public static async ValueTask<IRestResponse> UploadAsync(
        this HttpClient client,
        RestConfig config,
        string path,
        string filename,
        IDictionary<string, object>? headers = null,
        string? contentType = null,
        CompressOption compress = CompressOption.None,
        Action<long, long>? progress = null,
        CancellationToken cancel = default)
    {
        var fi = new FileInfo(filename);
#pragma warning disable CA2007
        await using var stream = fi.OpenRead();
#pragma warning restore CA2007
        return await UploadAsync(client, config, path, stream, headers, contentType, compress, progress, cancel).ConfigureAwait(false);
    }

    public static ValueTask<IRestResponse> UploadAsync(
        this HttpClient client,
        string path,
        Stream stream,
        IDictionary<string, object>? headers = null,
        string? contentType = null,
        CompressOption compress = CompressOption.None,
        Action<long, long>? progress = null,
        CancellationToken cancel = default)
    {
        return UploadAsync(client, RestConfig.Default, path, stream, headers, contentType, compress, progress, cancel);
    }

    public static async ValueTask<IRestResponse> UploadAsync(
        this HttpClient client,
        RestConfig config,
        string path,
        Stream stream,
        IDictionary<string, object>? headers = null,
        string? contentType = null,
        CompressOption compress = CompressOption.None,
        Action<long, long>? progress = null,
        CancellationToken cancel = default)
    {
        HttpResponseMessage? response = null;
#pragma warning disable CA1031
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);

            ProcessHeaders(request, headers);

            var progressProxy = progress is not null ? MakeProgress(stream, progress) : default;

            var content = new UploadStreamContent(stream, config.TransferBufferSize, compress, progressProxy, cancel);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? config.DefaultUploadContentType);
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
#pragma warning restore CA1031
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
