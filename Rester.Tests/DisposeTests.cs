namespace Rester;

using System.Net;

public sealed class DisposeTests
{
    private static RestConfig MakeConfig() => new RestConfig().UseJsonSerializer();

    [Fact]
    public async Task GetSuccessResponseDisposed()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, "{\"code\":\"1\"}"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        await client.GetAsync<SingleResponse>(config, "/single/1", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(handler.LastResponse);
        Assert.True(handler.LastResponse.WasDisposed);
    }

    [Fact]
    public async Task GetNonJsonResponseDisposed()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, null, "text/plain"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        await client.GetAsync<SingleResponse>(config, "/text", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(handler.LastResponse);
        Assert.True(handler.LastResponse.WasDisposed);
    }

    [Fact]
    public async Task Get400ResponseDisposed()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.BadRequest, "{\"error\":\"bad\"}"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        await client.GetAsync<SingleResponse>(config, "/error", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(handler.LastResponse);
        Assert.True(handler.LastResponse.WasDisposed);
    }

    [Fact]
    public async Task GetDeserializeFailsResponseDisposed()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, "{invalid"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        await client.GetAsync<SingleResponse>(config, "/broken", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(handler.LastResponse);
        Assert.True(handler.LastResponse.WasDisposed);
    }

    [Fact]
    public async Task PostSuccessResponseDisposed()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, null, "text/plain"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        await client.PostAsync(config, "/post", new PostRequest { Value = 200 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(handler.LastResponse);
        Assert.True(handler.LastResponse.WasDisposed);
    }

    [Fact]
    public async Task DownloadSuccessResponseDisposed()
    {
        // Arrange
        var data = new byte[256];
        using var handler = new TrackingHandler(_ => new TrackingResponse(HttpStatusCode.OK, null, "application/octet-stream")
        {
            Content = new ByteArrayContent(data)
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
#pragma warning disable CA2007
        await using var ms = new MemoryStream();
#pragma warning restore CA2007

        // Act
        await client.DownloadAsync(config, "/download", ms, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(handler.LastResponse);
        Assert.True(handler.LastResponse.WasDisposed);
    }

    [Fact]
    public async Task DownloadHttpErrorResponseDisposed()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.NotFound, null, "text/plain"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
#pragma warning disable CA2007
        await using var ms = new MemoryStream();
#pragma warning restore CA2007

        // Act
        await client.DownloadAsync(config, "/not-found", ms, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(handler.LastResponse);
        Assert.True(handler.LastResponse.WasDisposed);
    }

    [Fact]
    public async Task UploadSuccessResponseDisposed()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, null, "text/plain"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
#pragma warning disable CA2007
        await using var stream = new MemoryStream(new byte[128]);
#pragma warning restore CA2007

        // Act
        await client.UploadAsync(config, "/upload/test.dat", stream, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(handler.LastResponse);
        Assert.True(handler.LastResponse.WasDisposed);
    }

    [Fact]
    public async Task MultipartUploadSuccessResponseDisposed()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, null, "text/plain"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
#pragma warning disable CA2007
        await using var stream = new MemoryStream(new byte[128]);
#pragma warning restore CA2007

        // Act
        await client.MultipartUploadAsync(
            config,
            "/upload-multipart",
            stream,
            "file",
            "test.dat",
            cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.NotNull(handler.LastResponse);
        Assert.True(handler.LastResponse.WasDisposed);
    }
}
