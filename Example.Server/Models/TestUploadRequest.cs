namespace Example.Server.Models
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Http;

    public class TestUploadRequest
    {
        [AllowNull]
        public string Code { get; set; }

        [AllowNull]
        public string Tag { get; set; }

        public IFormFile? File1 { get; set; }

        public IFormFile? File2 { get; set; }
    }
}
