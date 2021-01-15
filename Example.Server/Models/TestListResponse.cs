namespace Example.Server.Models
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
        [AllowNull]
        public TestListResponseEntry[] Entries { get; set; }
    }
}
