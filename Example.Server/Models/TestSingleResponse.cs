namespace Example.Server.Models;

public sealed class TestSingleResponse
{
    public string Code { get; set; } = default!;

    public DateTimeOffset DateTime { get; set; }
}
