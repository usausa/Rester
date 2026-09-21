namespace Rester;

using System.Net;

[Collection("Server")]
public sealed class PutTests
{
    //--------------------------------------------------------------------------------
    // Test
    //--------------------------------------------------------------------------------

    private readonly ServerFixture fixture;

    public PutTests(ServerFixture fixture)
    {
        this.fixture = fixture;
    }

    private static RestConfig MakeConfig() => new RestConfig().UseJsonSerializer();

    [Fact]
    public async Task PutSuccess200()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.PutAsync(config, "/put", new PostRequest { Value = 200 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutNonGeneric400HttpError()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.PutAsync(config, "/put", new PostRequest { Value = 50 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.HttpError, response.RestResult);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutGenericSuccessContent()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.PutAsync<PostResponse>(config, "/put", new PostRequest { Value = 200 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("updated", response.Content?.Message);
    }

    [Fact]
    public async Task PutGeneric400HttpErrorNotSerializeError()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.PutAsync<PostResponse>(config, "/put", new PostRequest { Value = 50 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.HttpError, response.RestResult);
        Assert.Null(response.Content);
    }

    [Fact]
    public async Task PutTypeInfoGzipSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();
        var reqTypeInfo = PostJsonContext.Default.PostRequest;

        // Act
        var response = await client.PutAsync(config, "/put", new PostRequest { Value = 150 }, reqTypeInfo, compress: CompressOption.Gzip, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
    }

    [Fact]
    public async Task PutTypeInfoResponseSuccessContent()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();
        var reqTypeInfo = PostJsonContext.Default.PostRequest;
        var resTypeInfo = TestJsonContext.Default.PostResponse;

        // Act
        var response = await client.PutAsync(config, "/put", new PostRequest { Value = 150 }, reqTypeInfo, resTypeInfo, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("updated", response.Content?.Message);
    }

    [Fact]
    public async Task PutBufferedSuccess200()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();
        config.PostContentStreaming = false;

        // Act
        var response = await client.PutAsync(config, "/put", new PostRequest { Value = 200 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
    }

    [Fact]
    public async Task PutRequestMethodIsPut()
    {
        // Arrange
        HttpMethod? capturedMethod = null;
        using var handler = new TrackingHandler(req =>
        {
            capturedMethod = req.Method;
            return new TrackingResponse(HttpStatusCode.OK, null, "text/plain");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        var response = await client.PutAsync(config, "/put", new PostRequest { Value = 200 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(HttpMethod.Put, capturedMethod);
    }

    [Fact]
    public async Task PutDefaultConfigSuccess()
    {
        // Arrange
        HttpMethod? capturedMethod = null;
        using var handler = new TrackingHandler(req =>
        {
            capturedMethod = req.Method;
            return new TrackingResponse(HttpStatusCode.OK, null, "text/plain");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");

        // Act
        var response = await client.PutAsync("/put", new PostRequest { Value = 200 }, null, CompressOption.None, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(HttpMethod.Put, capturedMethod);
    }

    [Fact]
    public async Task PutStreamingSerializeErrorClassified()
    {
        // Arrange
        using var handler = new DrainHandler();
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        var response = await client.PutAsync(config, "/put", new BrokenRequest(), cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.SerializeError, response.RestResult);
        Assert.Equal((HttpStatusCode)0, response.StatusCode);
        Assert.IsType<InvalidOperationException>(response.InnerException);
    }

    [Fact]
    public async Task PutBufferedSerializeErrorClassified()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, null, "text/plain"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        config.PostContentStreaming = false;

        // Act
        var response = await client.PutAsync(config, "/put", new BrokenRequest(), cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.SerializeError, response.RestResult);
        Assert.Equal((HttpStatusCode)0, response.StatusCode);
        Assert.IsType<InvalidOperationException>(response.InnerException);
    }
}
