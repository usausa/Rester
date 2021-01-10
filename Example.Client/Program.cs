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
    using Rester.Transfer;

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
                    ? (long?)length
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
            await client.TestDownloadWithDecompressAsync().ConfigureAwait(false);

            // Upload
            await client.TestUploadAsync().ConfigureAwait(false);
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
            Console.WriteLine("==== GetAsync:Single ====");

            var response = await client.GetAsync<TestListResponse>("test/list?name=usa&count=5").ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
            Console.WriteLine($"Content.Entries.Length: {response.Content?.Entries.Length}");
            Console.WriteLine($"Content.Entries[0].Name: {response.Content?.Entries[0].Name}");
        }

        public async ValueTask TestGetWithHeaderAsync()
        {
            Console.WriteLine("==== GetAsync:Single ====");

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

            var response = await client.PostAsync("test/post", new TestPostRequest { Value = 100, Text = "うさうさ" }, compress: true).ConfigureAwait(false);

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

        public async ValueTask TestDownloadWithDecompressAsync()
        {
            Console.WriteLine("==== Download:Decompress ====");

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

            var response = await client.UploadAsync("test/upload", new MemoryStream(new byte[128]), "file", "test.dat").ConfigureAwait(false);

            Console.WriteLine($"Result: {response.RestResult}");
            Console.WriteLine($"StatusCode: {response.StatusCode}");
        }

        public async ValueTask TestUploadMultipleWithParameterAsync()
        {
            Console.WriteLine("==== Upload:Multiple:Parameter ====");

            var progress = -1d;
            var response = await client.UploadAsync(
                "test/upload2",
                new List<UploadEntry>
                {
                    new(new MemoryStream(new byte[128 * 1000]), "file1", "test.txt"),
                    new UploadEntry(new MemoryStream(new byte[128 * 1000]), "file2", "test.csv").WithGzip()
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
