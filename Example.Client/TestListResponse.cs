namespace Example.Client
{
    using System.Diagnostics.CodeAnalysis;

    public class TestListResponseEntry
    {
        public int No { get; set; }

        [AllowNull]
        public string Name { get; set; }
    }

    public class TestListResponse
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Performance")]
        [AllowNull]
        public TestListResponseEntry[] Entries { get; set; }
    }
}
