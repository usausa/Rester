namespace Example.Server.Models;

public class TestListResponseEntry
{
    public int No { get; set; }

    public string Name { get; set; } = default!;
}

#pragma warning disable CA1819
public class TestListResponse
{
    public TestListResponseEntry[] Entries { get; set; } = default!;
}
#pragma warning restore CA1819
