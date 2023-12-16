namespace Example.Server.Models;

using Microsoft.AspNetCore.Http;

public sealed class TestUploadRequest
{
    public string Code { get; set; } = default!;

    public string Tag { get; set; } = default!;

    public IFormFile? File1 { get; set; }

    public IFormFile? File2 { get; set; }
}
