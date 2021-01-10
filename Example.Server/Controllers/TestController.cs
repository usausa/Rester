namespace Example.Server.Controllers
{
    using System;
    using System.Linq;

    using Example.Server.Models;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    public class TestController : BaseApiController
    {
        private readonly ILogger<TestController> log;

        public TestController(ILogger<TestController> log)
        {
            this.log = log;
        }

        [HttpGet("{code}")]
        public IActionResult Single(string code)
        {
            return Ok(new TestSingleResponse { Code = code, DateTime = DateTimeOffset.UtcNow });
        }

        [HttpGet]
        public IActionResult List(TestListRequest request)
        {
            return Ok(new TestListResponse
            {
                Entries = Enumerable
                    .Range(1, Math.Min(request.Count ?? 10, 20))
                    .Select(x => new TestListResponseEntry { No = x, Name = $"{request.Name}-{x}" })
                    .ToArray()
            });
        }

        [HttpPost]
        public IActionResult Post([FromBody] TestPostRequest request)
        {
            return request.Value >= 100 ? (IActionResult)Ok() : BadRequest();
        }

        [HttpGet]
        public IActionResult Auth([FromHeader] string token)
        {
            if (String.IsNullOrEmpty(token))
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpGet("{filename}")]
        public IActionResult Download(string filename)
        {
            var size = 100 * 1000 * 1000;

            if (filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Response.Headers.Add("X-OriginalLength", $"{size}");

                return File(new byte[size], "application/json");
            }

            return File(new byte[size], "application/octet-stream");
        }

        [HttpPost]
        public IActionResult Upload(IFormFile file)
        {
            log.LogDebug($"File length ={file?.Length ?? 0}");

            if ((file?.Length ?? 0) < 100)
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpPost]
        public IActionResult Upload2(TestUploadRequest request)
        {
            log.LogDebug($"File1 length ={request.File1?.Length ?? 0}");
            log.LogDebug($"File2 length ={request.File2?.Length ?? 0}");

            if ((String.IsNullOrEmpty(request.Code) || String.IsNullOrEmpty(request.Tag)) ||
                ((request.File1?.Length ?? 0) < 100) || ((request.File2?.Length ?? 0) < 100))
            {
                return BadRequest();
            }

            return Ok();
        }
    }
}
