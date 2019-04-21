namespace Example.Server.Areas.Api.Models
{
    public class TestListResponseEntry
    {
        public int No { get; set; }

        public string Name { get; set; }
    }

    public class TestListResponse
    {
        public TestListResponseEntry[] Entries { get; set; }
    }
}
