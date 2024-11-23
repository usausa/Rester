using System.IO.Compression;
using System.Net.Mime;

using Example.Server.Infrastructure;

using Microsoft.AspNetCore.ResponseCompression;

#pragma warning disable CA1852
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers(static options =>
    {
        options.Conventions.Add(new LowercaseControllerModelConvention());
    })
    .AddJsonOptions(static options =>
    {
        options.JsonSerializerOptions.Converters.Add(new DateTimeOffsetConverter());
    });

// [Request]
builder.Services.AddRequestDecompression(static _ =>
{
    // Providers
});

// [Response]
builder.Services.AddResponseCompression(static options =>
{
    // Default false (for CRIME and BREACH attacks)
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = [MediaTypeNames.Application.Json];
});
builder.Services.Configure<BrotliCompressionProviderOptions>(static options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(static options =>
{
    options.Level = CompressionLevel.Fastest;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// [Request]
app.UseRequestDecompression();

// [Response]
app.UseResponseCompression();

app.MapControllers();

app.Run();
