namespace Rester;

[Collection("Server")]
public sealed class ClassificationTest
{
    private readonly ServerFixture fixture;

    public ClassificationTest(ServerFixture fixture)
    {
        this.fixture = fixture;
    }

    private static RestConfig MakeConfig() => new RestConfig().UseJsonSerializer();

    [Fact]
    public async Task PreCancelledReturnsCancel()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = MakeConfig();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(true);

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/single/1", cancel: cts.Token).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Cancel, response.RestResult);
    }

    [Fact]
    public async Task HttpClientTimeoutReturnsTimeout()
    {
        // Arrange
        var config = MakeConfig();
        using var handler = fixture.Server.CreateHandler();
        using var client = new HttpClient(handler);
        client.BaseAddress = fixture.Server.BaseAddress;
        client.Timeout = TimeSpan.FromMilliseconds(200);

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/delay", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Timeout, response.RestResult);
    }

    [Fact]
    public async Task ConnectionRefusedReturnsRequestError()
    {
        // Arrange
        var config = MakeConfig();
        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://127.0.0.1:1/");

        // Act
        var response = await client.GetAsync<SingleResponse>(config, "/nonexistent", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.RequestError, response.RestResult);
    }
}
