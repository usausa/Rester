namespace Example.Server.Models;

public sealed class TestPostRequest
{
    public int Value { get; set; }

    public string Text { get; set; } = default!;
}
