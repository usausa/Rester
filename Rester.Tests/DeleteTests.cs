namespace Rester;

using System.Net;

[Collection("Server")]
public sealed class DeleteTests
{
    //--------------------------------------------------------------------------------
    // Test
    //--------------------------------------------------------------------------------

    private readonly ServerFixture fixture;

    public DeleteTests(ServerFixture fixture)
    {
        this.fixture = fixture;
    }

    private static RestConfig MakeConfig() => new RestConfig().UseJsonSerializer();

    [Fact]
    public async Task DeleteReflectionSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.DeleteAsync<SingleResponse>(config, "/delete-json/42", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("42", response.Content?.Code);
    }

    [Fact]
    public async Task DeleteTypeInfoSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();
        var typeInfo = TestJsonContext.Default.SingleResponse;

        // Act
        var response = await client.DeleteAsync(config, "/delete-json/99", typeInfo, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("99", response.Content?.Code);
    }

    [Fact]
    public async Task DeleteNoContentSuccessContentIsDefault()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.DeleteAsync<SingleResponse>(config, "/delete/42", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(response.Content);
    }

    [Fact]
    public async Task DeleteNotFoundHttpErrorContentDefault()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.DeleteAsync<SingleResponse>(config, "/delete/missing", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.HttpError, response.RestResult);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.Content);
        Assert.Null(response.InnerException);
    }

    [Fact]
    public async Task DeleteRequestMethodIsDeleteWithoutContent()
    {
        // Arrange
        HttpMethod? capturedMethod = null;
        HttpContent? capturedContent = null;
        using var handler = new TrackingHandler(req =>
        {
            capturedMethod = req.Method;
            capturedContent = req.Content;
            return new TrackingResponse(HttpStatusCode.OK, "{\"code\":\"1\"}");
        });
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        var response = await client.DeleteAsync<SingleResponse>(config, "/delete-json/1", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(HttpMethod.Delete, capturedMethod);
        Assert.Null(capturedContent);
    }

    [Fact]
    public async Task DeleteDefaultConfigSuccess()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, "{\"code\":\"1\"}"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");

        // Act
        var response = await client.DeleteAsync<SingleResponse>("/delete-json/1", null, TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("1", response.Content?.Code);
    }

    [Fact]
    public async Task DeleteBrokenJsonSerializeError()
    {
        // Arrange
        using var handler = new TrackingHandler(static _ => new TrackingResponse(HttpStatusCode.OK, "{invalid"));
        using var client = new HttpClient(handler, disposeHandler: false);
        client.BaseAddress = new Uri("http://localhost/");
        var config = MakeConfig();

        // Act
        var response = await client.DeleteAsync<SingleResponse>(config, "/delete-json/1", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.SerializeError, response.RestResult);
    }
}
