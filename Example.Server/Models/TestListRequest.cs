namespace Example.Server.Models;

using System.Diagnostics.CodeAnalysis;

public class TestListRequest
{
    [AllowNull]
    public string Name { get; set; }

    public int? Count { get; set; }
}
