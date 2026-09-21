namespace Rester;

using System.Net;

[Collection("Server")]
public sealed class SendTests
{
    //--------------------------------------------------------------------------------
    // Test
    //--------------------------------------------------------------------------------

    private readonly ServerFixture fixture;

    public SendTests(ServerFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task SendDeleteSuccessNoContent()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.SendAsync(HttpMethod.Delete, "/delete/42", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SendDeleteNotFoundHttpError()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.SendAsync(HttpMethod.Delete, "/delete/missing", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.HttpError, response.RestResult);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.InnerException);
    }

    [Fact]
    public async Task SendJsonResponseNotReadSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.SendAsync(HttpMethod.Delete, "/delete-json/42", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendRequestMethodPassedWithoutContent()
    {
        // Arrange
        HttpMethod? capturedMethod = null;
        HttpContent? capturedContent = null;
        using var handler = new TrackingHandler(req =>
        {
            capturedMethod = req.Method;
            capturedContent = req.Content;
            return new TrackingResponse(HttpStatusCode.OK, null, "text/plain");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");

        // Act
        var response = await client.SendAsync(HttpMethod.Head, "/single/1", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(HttpMethod.Head, capturedMethod);
        Assert.Null(capturedContent);
    }

    [Fact]
    public async Task SendRequestHeaderAddedToRequestHeaders()
    {
        // Arrange
        string? captured = null;
        using var handler = new TrackingHandler(req =>
        {
            captured = req.Headers.TryGetValues("X-Custom", out var values) ? String.Join(",", values) : null;
            return new TrackingResponse(HttpStatusCode.NoContent, null, "text/plain");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var headers = new Dictionary<string, object>
        {
            { "X-Custom", "abc" }
        };

        // Act
        var response = await client.SendAsync(HttpMethod.Delete, "/delete/1", headers, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("abc", captured);
    }

    [Fact]
    public async Task SendInvalidHeaderNameUnknown()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.NoContent, null, "text/plain"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var headers = new Dictionary<string, object>
        {
            { "bad name", "value" }
        };

        // Act
        var response = await client.SendAsync(HttpMethod.Delete, "/delete/1", headers, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Unknown, response.RestResult);
        Assert.IsType<ArgumentException>(response.InnerException);
    }

    [Fact]
    public async Task SendPreCancelledReturnsCancel()
    {
        // Arrange
        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://127.0.0.1:1/");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(true);

        // Act
        var response = await client.SendAsync(HttpMethod.Delete, "/delete/1", cancel: cts.Token).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Cancel, response.RestResult);
    }
}
