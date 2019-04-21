namespace Example.Server.Areas.Api.Models
{
    using Microsoft.AspNetCore.Http;

    public class TestUploadRequest
    {
        public string Code { get; set; }

        public string Tag { get; set; }

        public IFormFile File1 { get; set; }

        public IFormFile File2 { get; set; }
    }
}
