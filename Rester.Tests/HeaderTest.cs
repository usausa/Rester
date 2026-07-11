namespace Rester;

using System.Net;

public sealed class HeaderTest
{
    //--------------------------------------------------------------------------------
    // Test
    //--------------------------------------------------------------------------------

    private static readonly string[] MultiValues = ["v1", "v2"];

    private static RestConfig MakeConfig() => new RestConfig().UseJsonSerializer();

    [Fact]
    public async Task RequestHeaderAddedToRequestHeaders()
    {
        // Arrange
        string? captured = null;
        using var handler = new TrackingHandler(req =>
        {
            captured = req.Headers.TryGetValues("X-Custom", out var values) ? string.Join(",", values) : null;
            return new TrackingResponse(HttpStatusCode.OK, "{\"code\":\"1\"}");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        var headers = new Dictionary<string, object>
        {
            { "X-Custom", "abc" }
        };

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/single/1", headers, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("abc", captured);
    }

    [Fact]
    public async Task MultiValueHeaderAddedToRequestHeaders()
    {
        // Arrange
        string? captured = null;
        using var handler = new TrackingHandler(req =>
        {
            captured = req.Headers.TryGetValues("X-Custom", out var values) ? string.Join(",", values) : null;
            return new TrackingResponse(HttpStatusCode.OK, "{\"code\":\"1\"}");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        var headers = new Dictionary<string, object>
        {
            { "X-Custom", MultiValues }
        };

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/single/1", headers, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("v1,v2", captured);
    }

    [Fact]
    public async Task ObjectEnumerableHeaderAddedToRequestHeaders()
    {
        // Arrange
        string? captured = null;
        using var handler = new TrackingHandler(req =>
        {
            captured = req.Headers.TryGetValues("X-Custom", out var values) ? string.Join(",", values) : null;
            return new TrackingResponse(HttpStatusCode.OK, "{\"code\":\"1\"}");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        var headers = new Dictionary<string, object>
        {
            { "X-Custom", new object[] { 1, 2 } }
        };

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/single/1", headers, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("1,2", captured);
    }

    [Fact]
    public async Task ContentHeaderRoutedToContentHeaders()
    {
        // Arrange
        string? captured = null;
        using var handler = new TrackingHandler(req =>
        {
            captured = req.Content?.Headers.ContentLanguage.FirstOrDefault();
            return new TrackingResponse(HttpStatusCode.OK, null, "text/plain");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        var headers = new Dictionary<string, object>
        {
            { "Content-Language", "ja" }
        };

        // Act
        var response = await client.PostAsync(config, "/post", new PostRequest { Value = 200 }, headers, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("ja", captured);
    }

    [Fact]
    public async Task ContentHeaderWithoutContentUnknown()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, "{\"code\":\"1\"}"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        var headers = new Dictionary<string, object>
        {
            { "Content-Language", "ja" }
        };

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/single/1", headers, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Unknown, response.RestResult);
        Assert.IsType<ArgumentException>(response.InnerException);
    }

    [Fact]
    public async Task NullHeaderValueUnknown()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, "{\"code\":\"1\"}"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        var headers = new Dictionary<string, object>
        {
            { "X-Custom", null! }
        };

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/single/1", headers, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Unknown, response.RestResult);
        Assert.IsType<ArgumentException>(response.InnerException);
    }

    [Fact]
    public async Task InvalidHeaderNameUnknown()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, "{\"code\":\"1\"}"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();
        var headers = new Dictionary<string, object>
        {
            { "bad name", "value" }
        };

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/single/1", headers, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Unknown, response.RestResult);
        Assert.IsType<ArgumentException>(response.InnerException);
    }
}
