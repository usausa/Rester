namespace Rester;

using System.Net;

public sealed class HeaderTests
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
            captured = req.Headers.TryGetValues("X-Custom", out var values) ? String.Join(",", values) : null;
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
            captured = req.Headers.TryGetValues("X-Custom", out var values) ? String.Join(",", values) : null;
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
            captured = req.Headers.TryGetValues("X-Custom", out var values) ? String.Join(",", values) : null;
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
    //--------------------------------------------------------------------------------
    // Response headers
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task ResponseHeadersAvailableOnSuccess()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ =>
        {
            var response = new TrackingResponse(HttpStatusCode.OK, "{\"code\":\"1\"}");
            response.Headers.Add("X-Custom", "abc");
            return response;
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/single/1", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.NotNull(handler.LastResponse);
        Assert.True(handler.LastResponse.WasDisposed);
        Assert.NotNull(response.Headers);
        Assert.Equal("abc", response.Headers.GetValues("X-Custom").First());
    }

    [Fact]
    public async Task ResponseHeadersAvailableOnHttpError()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ =>
        {
            var response = new TrackingResponse(HttpStatusCode.Conflict, "{\"error\":\"conflict\"}");
            response.Headers.Location = new Uri("http://localhost/single/1");
            return response;
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        var response = await client.PostAsync(config, "/post", new PostRequest { Value = 200 }, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.HttpError, response.RestResult);
        Assert.Equal(new Uri("http://localhost/single/1"), response.Headers?.Location);
    }

    [Fact]
    public async Task ResponseHeadersAvailableOnSerializeError()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ =>
        {
            var response = new TrackingResponse(HttpStatusCode.OK, "{invalid");
            response.Headers.Add("X-Custom", "abc");
            return response;
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/broken", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.SerializeError, response.RestResult);
        Assert.Equal("abc", response.Headers?.GetValues("X-Custom").First());
    }

    [Fact]
    public async Task ResponseHeadersNullWithoutResponse()
    {
        // Arrange
        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://127.0.0.1:1/");
        var config = MakeConfig();

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/nonexistent", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.RequestError, response.RestResult);
        Assert.Null(response.Headers);
    }
}
