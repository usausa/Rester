namespace Example.Client;

using System;

public sealed class TestSingleResponse
{
    public string Code { get; set; } = default!;

    public DateTimeOffset DateTime { get; set; }
}
