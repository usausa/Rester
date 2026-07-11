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
    public async Task PostBufferedSuccess200()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();
        config.PostContentStreaming = false;

        // Act
        var response = await client.PostAsync(config, "/post", new PostRequest { Value = 200 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
    }

    [Fact]
    public async Task PostStreamingNoContentLength()
    {
        // Arrange
        long? capturedLength = -1;
        using var handler = new TrackingHandler(req =>
        {
            capturedLength = req.Content?.Headers.ContentLength;
            return new TrackingResponse(System.Net.HttpStatusCode.OK, null, "text/plain");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        var response = await client.PostAsync(config, "/post", new PostRequest { Value = 200 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Null(capturedLength);
    }

    [Fact]
    public async Task PostBufferedContentLengthSet()
    {
        // Arrange
        long? capturedLength = null;
        using var handler = new TrackingHandler(req =>
        {
            capturedLength = req.Content?.Headers.ContentLength;
            return new TrackingResponse(System.Net.HttpStatusCode.OK, null, "text/plain");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        config.PostContentStreaming = false;

        // Act
        var response = await client.PostAsync(config, "/post", new PostRequest { Value = 200 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.True(capturedLength > 0);
    }

    [Fact]
    public async Task PostStreamingSerializeErrorClassified()
    {
        // Arrange
        using var handler = new DrainHandler();
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        var response = await client.PostAsync(config, "/post", new BrokenRequest(), cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.SerializeError, response.RestResult);
        Assert.Equal((System.Net.HttpStatusCode)0, response.StatusCode);
        Assert.IsType<InvalidOperationException>(response.InnerException);
    }

    [Fact]
    public async Task PostBufferedSerializeErrorClassified()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(System.Net.HttpStatusCode.OK, null, "text/plain"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        config.PostContentStreaming = false;

        // Act
        var response = await client.PostAsync(config, "/post", new BrokenRequest(), cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.SerializeError, response.RestResult);
        Assert.Equal((System.Net.HttpStatusCode)0, response.StatusCode);
        Assert.IsType<InvalidOperationException>(response.InnerException);
    }

    [Fact]
    public async Task PostBufferedCompressedCancelReturnsCancel()
    {
        // Arrange
        // Cancellation is requested by the handler before the request content is copied,
        // verifying that CompressedContent propagates the cancellation token
        using var cts = new CancellationTokenSource();
        using var handler = new DrainHandler(() => cts.Cancel());
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        config.PostContentStreaming = false;
        var parameter = new { Values = Enumerable.Range(0, 10000).Select(static i => $"value-{i}").ToArray() };

        // Act
        var response = await client.PostAsync(config, "/post", parameter, compress: CompressOption.Gzip, cancel: cts.Token).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Cancel, response.RestResult);
    }

    [Fact]
    public async Task PostStreamingCompressedCancelReturnsCancel()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        using var handler = new DrainHandler(() => cts.Cancel());
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        var parameter = new { Values = Enumerable.Range(0, 10000).Select(static i => $"value-{i}").ToArray() };

        // Act
        var response = await client.PostAsync(config, "/post", parameter, compress: CompressOption.Gzip, cancel: cts.Token).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Cancel, response.RestResult);
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
