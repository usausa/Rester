namespace Example.Client;

public sealed class TestListResponseEntry
{
    public int No { get; set; }

    public string Name { get; set; } = default!;
}

#pragma warning disable CA1819
public sealed class TestListResponse
{
    public TestListResponseEntry[] Entries { get; set; } = default!;
}
#pragma warning restore CA1819
