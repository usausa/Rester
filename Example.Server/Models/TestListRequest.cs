namespace Example.Server.Models;

public sealed class TestListRequest
{
    public string Name { get; set; } = default!;

    public int? Count { get; set; }
}
