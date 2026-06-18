namespace Rester;

[Collection("Server")]
public sealed class PostTests
{
    //--------------------------------------------------------------------------------
    // Test
    //--------------------------------------------------------------------------------

    private readonly ServerFixture fixture;

    public PostTests(ServerFixture fixture)
    {
        this.fixture = fixture;
    }

    private static RestConfig MakeConfig() => new RestConfig().UseJsonSerializer();

    [Fact]
    public async Task PostSuccess200()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.PostAsync(config, "/post", new PostRequest { Value = 200 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostNonGeneric400HttpError()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.PostAsync(config, "/post", new PostRequest { Value = 50 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.HttpError, response.RestResult);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostGeneric400HttpErrorNotSerializeError()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.PostAsync<PostResponse>(config, "/post", new PostRequest { Value = 50 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.HttpError, response.RestResult);
        Assert.Null(response.Content);
    }

    [Fact]
    public async Task PostTypeInfoGzipSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();
        var reqTypeInfo = PostJsonContext.Default.PostRequest;

        // Act
        var response = await client.PostAsync(config, "/post", new PostRequest { Value = 150 }, reqTypeInfo, compress: CompressOption.Gzip, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
    }

    [Fact]
    public async Task PostGzipCompressSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.PostAsync(config, "/post", new PostRequest { Value = 200 }, compress: CompressOption.Gzip, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
    }

    [Fact]
    public async Task PostDeflateContentEncodingSet()
    {
        // Arrange
        // Verify that CompressOption.Deflate sets Content-Encoding header properly
        // via a mock handler (TestServer does not support deflate decompression by default)
        string? capturedEncoding = null;
        using var handler = new TrackingHandler(req =>
        {
            capturedEncoding = req.Content?.Headers.ContentEncoding.FirstOrDefault();
            return new TrackingResponse(System.Net.HttpStatusCode.OK, null, "text/plain");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        var response = await client.PostAsync(config, "/post", new PostRequest { Value = 200 }, compress: CompressOption.Deflate, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("deflate", capturedEncoding);
    }
}
