namespace Rester;

[Collection("Server")]
public sealed class DownloadTests
{
    private readonly ServerFixture fixture;

    public DownloadTests(ServerFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task DownloadStreamSuccessBytesMatch()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = new RestConfig().UseJsonSerializer();
#pragma warning disable CA2007
        await using var ms = new MemoryStream();
#pragma warning restore CA2007
        long lastProcessed = 0, lastTotal = 0;

        // Act
        var response = await client.DownloadAsync(
            config,
            "/download",
            ms,
            progress: (p, t) =>
            {
                lastProcessed = p;
                lastTotal = t;
            },
            cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(64 * 1024, ms.Length);
        Assert.Equal(lastTotal, lastProcessed);
    }

    [Fact]
    public async Task DownloadNoLengthWithLengthResolverProgressHasTotal()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = new RestConfig().UseJsonSerializer();
        config.LengthResolver = static ctx =>
        {
            var val = ctx.GetValues("X-OriginalLength").FirstOrDefault();
            return long.TryParse(val, out var len) ? len : null;
        };
#pragma warning disable CA2007
        await using var ms = new MemoryStream();
#pragma warning restore CA2007
        long capturedTotal = 0;

        // Act
        var response = await client.DownloadAsync(
            config,
            "/download-no-length",
            ms,
            progress: (_, t) => { capturedTotal = t; },
            cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(64 * 1024, capturedTotal);
    }

    [Fact]
    public async Task DownloadNoLengthNoResolverSuccess()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = new RestConfig().UseJsonSerializer();
#pragma warning disable CA2007
        await using var ms = new MemoryStream();
#pragma warning restore CA2007

        // Act
        var response = await client.DownloadAsync(
            config,
            "/download-no-length",
            ms,
            cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
        Assert.Equal(64 * 1024, ms.Length);
    }

    [Fact]
    public async Task DownloadFilenameSuccessFileCreated()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = new RestConfig().UseJsonSerializer();
        var tmpFile = Path.Combine(Path.GetTempPath(), $"rester_test_{Guid.NewGuid():N}.dat");
        try
        {
            // Act
            var response = await client.DownloadAsync(
                config,
                "/download",
                tmpFile,
                cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Assert
            Assert.Equal(RestResult.Success, response.RestResult);
            Assert.True(File.Exists(tmpFile));
            var info = new FileInfo(tmpFile);
            Assert.Equal(64 * 1024, info.Length);
        }
        finally
        {
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }
        }
    }

    [Fact]
    public async Task DownloadFilenameHttpErrorFileNotLeft()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = new RestConfig().UseJsonSerializer();
        var tmpFile = Path.Combine(Path.GetTempPath(), $"rester_test_{Guid.NewGuid():N}.dat");
        try
        {
            // Act
            var response = await client.DownloadAsync(
                config,
                "/nonexistent-404",
                tmpFile,
                cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

            // Assert
            Assert.Equal(RestResult.HttpError, response.RestResult);
            Assert.False(File.Exists(tmpFile));
        }
        finally
        {
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }
        }
    }
}
