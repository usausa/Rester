namespace Example.Server.Controllers;

using System.Buffers;

using Example.Server.Infrastructure;
using Example.Server.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1848
#pragma warning disable ASP0023
public sealed class TestController : BaseApiController
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

    [HttpGet]
    public IActionResult Auth([FromHeader] string token)
    {
        if (String.IsNullOrEmpty(token))
        {
            return BadRequest();
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult Post([FromBody] TestPostRequest request)
    {
        return request.Value >= 100 ? Ok() : BadRequest();
    }

    [HttpGet("{filename}")]
    public async Task Download(string filename)
    {
        const int size = 100 * 1000 * 1000;

        if (filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            Response.Headers.Append("X-OriginalLength", $"{size}");
            Response.ContentType = "application/json";
        }
        else
        {
            Response.ContentType = "application/octet-stream";
        }

        Response.ContentLength = size;

#pragma warning disable CA2007
        await using var stream = new PatternStream(size);
#pragma warning restore CA2007
        await stream.CopyToAsync(Response.Body, HttpContext.RequestAborted).ConfigureAwait(false);
    }

    [HttpPost("{filename}")]
    [ReadableBodyStream]
    public async ValueTask<IActionResult> Upload(string filename)
    {
        var length = 0L;
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            int read;
            while ((read = await Request.Body.ReadAsync(buffer.AsMemory(), HttpContext.RequestAborted).ConfigureAwait(false)) > 0)
            {
                length += read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        log.LogDebug("Request filename={Filename}, length={Length}", filename, length);

        return Ok();
    }

    [HttpPost]
    public IActionResult Upload2(IFormFile? file)
    {
        log.LogDebug("File length={Length}", file?.Length ?? 0);

        if ((file?.Length ?? 0) < 100)
        {
            return BadRequest();
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult Upload3(TestUploadRequest request)
    {
        log.LogDebug("File1 length={Length}", request.File1?.Length ?? 0);
        log.LogDebug("File2 length={Length}", request.File2?.Length ?? 0);

        if (String.IsNullOrEmpty(request.Code) ||
            String.IsNullOrEmpty(request.Tag) ||
            ((request.File1?.Length ?? 0) < 100) || ((request.File2?.Length ?? 0) < 100))
        {
            return BadRequest();
        }

        return Ok();
    }
}
#pragma warning restore ASP0023
#pragma warning restore CA1848
