namespace Example.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Rester;

    public static class Program
    {
        public static async Task Main()
        {
            RestConfig.Default.UseJsonSerializer(options =>
            {
                options.Converters.Add(new DateTimeOffsetConverter());
            });
            RestConfig.Default.LengthResolver = ctx =>
                Int64.TryParse(ctx.GetValues("X-OriginalLength").FirstOrDefault(), out var length)
                    ? length
                    : null;

            var client = new TestClient("https://localhost:44334/");

            // Get
            await client.TestGetSingleAsync().ConfigureAwait(false);
            await client.TestGetListAsync().ConfigureAwait(false);
            await client.TestGetWithHeaderAsync().ConfigureAwait(false);

            // Post
            await client.TestPostAsync().ConfigureAwait(false);
            await client.TestPostWithCompressAsync().ConfigureAwait(false);

            // Download
            await client.TestDownloadAsync().ConfigureAwait(false);
            await client.TestDownloadWithCompressAsync().ConfigureAwait(false);

            // Upload
            await client.TestUploadAsync().ConfigureAwait(false);
            await client.TestUploadWithCompressAsync().ConfigureAwait(false);

            // Upload2
            await client.TestMultipartUploadAsync().ConfigureAwait(false);
            await client.TestUploadMultipleWithParameterAsync().ConfigureAwait(false);
        }
    }

    public sealed class TestClient : IDisposable
    {
        private readonly HttpClient client;

        public TestClient(string address)
        {
            client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            {
                BaseAddress = new Uri(address),
                Timeout = new TimeSpan(0, 0, 30, 0)
            };
        }

        public void Dispose()
        {
            client.Dispose();
        }

        // Get

        public async ValueTask TestGetSingleAsync()
        {
            Console.WriteLine("==== GetAsync:Single ====");

            var response = await client.GetAsync<TestSingleResponse>("test/single/123").ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
            Console.WriteLine($"Content.Code: {response.Content?.Code}");
        }

        public async ValueTask TestGetListAsync()
        {
            Console.WriteLine("==== GetAsync:List ====");

            var response = await client.GetAsync<TestListResponse>("test/list?name=usa&count=5").ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
            Console.WriteLine($"Content.Entries.Length: {response.Content?.Entries.Length}");
            Console.WriteLine($"Content.Entries[0].Name: {response.Content?.Entries[0].Name}");
        }

        public async ValueTask TestGetWithHeaderAsync()
        {
            Console.WriteLine("==== GetAsync:Auth ====");

            // BadRequest
            var response = await client.GetAsync<TestSingleResponse>("test/auth").ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");

            var headers = new Dictionary<string, object>
            {
                { "token", "1234567890" }
            };
            response = await client.GetAsync<TestSingleResponse>("test/auth", headers).ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
        }

        // Post

        public async ValueTask TestPostAsync()
        {
            Console.WriteLine("==== PostAsync ====");

            // BadRequest
            var response = await client.PostAsync("test/post", new TestPostRequest { Value = 1, Text = "うさうさ" }).ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");

            response = await client.PostAsync("test/post", new TestPostRequest { Value = 100, Text = "うさうさ" }).ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
        }

        public async ValueTask TestPostWithCompressAsync()
        {
            Console.WriteLine("==== PostAsync:Compress ====");

            var response = await client.PostAsync("test/post", new TestPostRequest { Value = 100, Text = "うさうさ" }, compress: CompressOption.Gzip).ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
        }

        // Download

        public async ValueTask TestDownloadAsync()
        {
            Console.WriteLine("==== Download ====");

            var progress = -1d;
            var response = await client.DownloadAsync(
                "test/download/test.dat",
                "test.dat",
                progress: (processed, total) =>
                {
                    var percent = Math.Floor((double)processed / total * 100);
                    if (percent > progress)
                    {
                        progress = percent;
                        Console.WriteLine($"{processed} / {total} : {progress}");
                    }
                }).ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
        }

        public async ValueTask TestDownloadWithCompressAsync()
        {
            Console.WriteLine("==== Download:Compress ====");

            var progress = -1d;
            var response = await client.DownloadAsync(
                "test/download/test.json",
                "test.json",
                progress: (processed, total) =>
                {
                    var percent = Math.Floor((double)processed / total * 100);
                    if (percent > progress)
                    {
                        progress = percent;
                        Console.WriteLine($"{processed} / {total} : {progress}");
                    }
                }).ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
        }

        // Upload

        public async ValueTask TestUploadAsync()
        {
            Console.WriteLine("==== Upload ====");

#pragma warning disable CA2007
            await using var stream = new MemoryStream(Enumerable.Range(0, 1 * 1000 * 1000).Select(x => (byte)(x % 256)).ToArray());
#pragma warning restore CA2007
            var progress = -1d;
            var response = await client.UploadAsync(
                "test/upload/test.dat",
                stream,
                progress: (processed, total) =>
                {
                    var percent = Math.Floor((double)processed / total * 100);
                    if (percent > progress)
                    {
                        progress = percent;
                        Console.WriteLine($"{processed} / {total} : {progress}");
                    }
                }).ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
        }

        public async ValueTask TestUploadWithCompressAsync()
        {
            Console.WriteLine("==== Upload:Compress ====");

#pragma warning disable CA2007
            await using var stream = new MemoryStream(Enumerable.Range(0, 1 * 1000 * 1000).Select(x => (byte)(x % 256)).ToArray());
#pragma warning restore CA2007
            var progress = -1d;
            var response = await client.UploadAsync(
                "test/upload/test.dat",
                stream,
                compress: CompressOption.Gzip,
                progress: (processed, total) =>
                {
                    var percent = Math.Floor((double)processed / total * 100);
                    if (percent > progress)
                    {
                        progress = percent;
                        Console.WriteLine($"{processed} / {total} : {progress}");
                    }
                }).ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
        }

        public async ValueTask TestMultipartUploadAsync()
        {
            Console.WriteLine("==== Upload ====");

            var response = await client.MultipartUploadAsync("test/upload2", new MemoryStream(new byte[128]), "file", "test.dat").ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
        }

        public async ValueTask TestUploadMultipleWithParameterAsync()
        {
            Console.WriteLine("==== Upload:Multiple:Parameter ====");

            var progress = -1d;
            var response = await client.MultipartUploadAsync(
                "test/upload3",
                new List<MultipartUploadEntry>
                {
                    new(new MemoryStream(new byte[128 * 1000]), "file1", "test.txt"),
                    new(new MemoryStream(new byte[128 * 1000]), "file2", "test.csv", CompressOption.Gzip)
                },
                new Dictionary<string, object>
                {
                    { "Code", 123 },
                    { "Tag", "abc" }
                },
                progress: (processed, total) =>
                {
                    var percent = Math.Floor((double)processed / total * 100);
                    if (percent > progress)
                    {
                        progress = percent;
                        Console.WriteLine($"{processed} / {total} : {progress}");
                    }
                }).ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
        }
    }
}
