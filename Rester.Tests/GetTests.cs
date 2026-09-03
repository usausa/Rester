namespace Rester;

[Collection("Server")]
public sealed class GetTests
{
    //--------------------------------------------------------------------------------
    // Test
    //--------------------------------------------------------------------------------

    private readonly ServerFixture fixture;

    public GetTests(ServerFixture fixture)
    {
        this.fixture = fixture;
    }

    private static RestConfig MakeConfig() => new RestConfig().UseJsonSerializer();

    [Fact]
    public async Task GetSingleReflectionSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/single/42", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("42", response.Content?.Code);
    }

    [Fact]
    public async Task GetSingleTypeInfoSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();
        var typeInfo = TestJsonContext.Default.SingleResponse;

        // Act
        var response = await client.GetAsync(config, "/single/99", typeInfo, cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal("99", response.Content?.Code);
    }

    [Fact]
    public async Task GetListSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.GetAsync<ListResponse>(config, "/list?name=test&count=3", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(3, response.Content?.Entries?.Length);
    }

    [Fact]
    public async Task GetLargeJsonSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.GetAsync<ListResponse>(config, "/large-list?count=5000", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(5000, response.Content?.Entries?.Length);
        Assert.Equal(5000, response.Content?.Entries?[^1].No);
    }

    [Fact]
    public async Task GetCancelDuringBodyReturnsCancel()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // Act
        var response = await client.GetAsync<ListResponse>(config, "/slow-json", cancel: cts.Token).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Cancel, response.RestResult);
    }

    [Fact]
    public async Task GetTextSuccessContentIsDefault()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/text", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Null(response.Content);
    }

    [Fact]
    public async Task GetBrokenJsonSerializeError()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/broken-json", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.SerializeError, response.RestResult);
    }

    [Fact]
    public async Task GetErrorJsonHttpErrorContentDefaultNoInnerException()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/error-json", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.HttpError, response.RestResult);
        Assert.Null(response.Content);
        Assert.Null(response.InnerException);
    }

    [Fact]
    public async Task GetNotFoundHttpError()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/nonexistent-path-404", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.HttpError, response.RestResult);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
