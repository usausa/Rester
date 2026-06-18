namespace Rester;

using System.Net.Http;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class ServerFixture : IAsyncLifetime
{
    private WebApplication? app;

    public TestServer Server { get; private set; } = default!;

    public HttpClient CreateClient() => Server.CreateClient();

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRequestDecompression();

        app = builder.Build();
        app.UseRequestDecompression();

        app.MapGet("/single/{code}", (string code) =>
            Results.Json(new { code, value = $"value-{code}" }));

        app.MapGet("/list", (string? name, int? count) =>
        {
            var n = Math.Min(count ?? 5, 20);
            var entries = Enumerable.Range(1, n)
                .Select(i => new { no = i, name = $"{name}-{i}" })
                .ToArray();
            return Results.Json(new { entries });
        });

        app.MapPost("/post", async (HttpRequest request) =>
        {
            var body = await JsonDocument.ParseAsync(request.Body).ConfigureAwait(true);
            // Accept both "value" (camelCase) and "Value" (PascalCase from JsonSerializer default)
            var found = body.RootElement.TryGetProperty("value", out var valLower) ||
                        body.RootElement.TryGetProperty("Value", out valLower);
            if (found && valLower.GetInt32() >= 100)
            {
                return Results.Ok(new { message = "ok" });
            }

            return Results.Json(new { error = "bad value" }, statusCode: 400);
        });

        app.MapGet("/text", () => Results.Text("hello"));

        app.MapGet("/broken-json", () => Results.Content("{invalid", "application/json"));

        app.MapGet("/error-json", () =>
            Results.Json(new { error = "server error" }, statusCode: 500));

        app.MapGet("/delay", async (HttpContext ctx) =>
        {
            await Task.Delay(5000, ctx.RequestAborted).ConfigureAwait(true);
            return Results.Ok();
        });

        app.MapGet("/download", () =>
        {
            var data = new byte[64 * 1024];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256);
            }

            return Results.Bytes(data, "application/octet-stream");
        });

        app.MapGet("/download-no-length", async ctx =>
        {
            var data = new byte[64 * 1024];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256);
            }

            ctx.Response.Headers["X-OriginalLength"] = data.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ctx.Response.ContentType = "application/octet-stream";
            await ctx.Response.Body.WriteAsync(data.AsMemory()).ConfigureAwait(true);
        });

        app.MapPost("/upload/{filename}", async (HttpRequest request) =>
        {
#pragma warning disable CA2007
            await using var ms = new MemoryStream();
#pragma warning restore CA2007
            await request.Body.CopyToAsync(ms).ConfigureAwait(true);
            return Results.Json(new { length = ms.Length });
        });

        app.MapPost("/upload-multipart", async (HttpRequest request) =>
        {
            var form = await request.ReadFormAsync().ConfigureAwait(true);
            var files = form.Files.Count;
            var totalLength = form.Files.Sum(static f => f.Length);
            var fields = form.Keys.Count;
            return Results.Json(new { files, totalLength, fields });
        }).DisableAntiforgery();

        await app.StartAsync().ConfigureAwait(true);
        Server = app.GetTestServer();
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.StopAsync().ConfigureAwait(true);
            await app.DisposeAsync().ConfigureAwait(true);
        }
    }
}
