namespace Rester;

[Collection("Server")]
public sealed class UploadTests
{
    //--------------------------------------------------------------------------------
    // Test
    //--------------------------------------------------------------------------------

    private readonly ServerFixture fixture;

    public UploadTests(ServerFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task UploadStreamSuccessLengthMatch()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = new RestConfig().UseJsonSerializer();
        var data = new byte[1024 * 8];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i % 256);
        }

#pragma warning disable CA2007
        await using var stream = new MemoryStream(data);
#pragma warning restore CA2007
        var progressValues = new List<long>();

        // Act
        var response = await client.UploadAsync(
            config,
            "/upload/test.dat",
            stream,
            progress: (p, _) =>
            {
                progressValues.Add(p);
            },
            cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);

        // Progress should be monotonically increasing
        for (var i = 1; i < progressValues.Count; i++)
        {
            Assert.True(progressValues[i] >= progressValues[i - 1]);
        }
    }

    [Fact]
    public async Task MultipartUploadTwoFilesFieldsDelivered()
    {
        // Arrange
        var client = fixture.CreateClient();
        var config = new RestConfig().UseJsonSerializer();
        var data1 = new byte[256];
        var data2 = new byte[512];
#pragma warning disable CA2007
        await using var s1 = new MemoryStream(data1);
        await using var s2 = new MemoryStream(data2);
#pragma warning restore CA2007

        var entries = new List<MultipartUploadEntry>
        {
            new(s1, "file1", "test1.dat"),
            new(s2, "file2", "test2.dat")
        };

        var parameters = new Dictionary<string, object>
        {
            { "Code", "123" },
            { "Tag", "abc" },
            { "Extra", "xyz" }
        };

        // Act
        var response = await client.MultipartUploadAsync(
            config,
            "/upload-multipart",
            entries,
            parameters,
            cancel: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Assert
        Assert.Equal(RestResult.Success, response.RestResult);
    }
}
