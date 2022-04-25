namespace Example.Client;

public class TestListResponseEntry
{
    public int No { get; set; }

    public string Name { get; set; } = default!;
}

public class TestListResponse
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Performance")]
    public TestListResponseEntry[] Entries { get; set; } = default!;
}
