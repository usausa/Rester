namespace Rester;

[Collection("Server")]
public sealed class PoolTests
{
    //--------------------------------------------------------------------------------
    // Test
    //--------------------------------------------------------------------------------

    private readonly ServerFixture fixture;

    public PoolTests(ServerFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task HundredSequentialGetTextAllSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = new RestConfig().UseJsonSerializer();

        // Act & Assert
        for (var i = 0; i < 100; i++)
        {
            var response = await client.GetAsync<SingleResponse>(config, "/text", cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(response.RestResult == RestResult.Success, $"Request {i} failed: {response.RestResult}");
        }
    }
}
