# Rester - HttpClient extensions for rest api

[![NuGet](https://img.shields.io/nuget/v/Rester.svg)](https://www.nuget.org/packages/Rester/)

## What is this?

* HttpClient extensions with serialization function
* Download/Upload helper api with progress

### Usage example

```csharp
// Config
RestConfig.Default.UseJsonSerializer();

// Prepare HttpClient
var client = new HttpClient();

// Get
var response = await client.GetAsync<TestGetResponse>("api/test/get/123");
// Check result and get TestGetResponse
var result = response.IsSuccess();
var content = response.Content; 

// Post with compress
var response = await client.PostAsync("api/test/post", new TestPostRequest { Data = "..." }, compress: CompressOption.Gzip);

// Download with progress
var response = await client.DownloadAsync(
    "api/test/download/test.dat",
    "test.dat",
    progress: (processed, total) =>
    {
        ...
    });

// Multiple file upload with other parameter and progress
var response = await client.MultipartUploadAsync(
    "api/test/upload",
    new List<MultipartUploadEntry>
    {
        new MultipartUploadEntry(stream1, "file1", "test.txt"),
        new MultipartUploadEntry(stream2, "file2", "test.csv", CompressOption.Gzip)
    },
    new Dictionary<string, object>
    {
        { "Code", 123 },
        { "Tag", "abc" }
    },
    progress: (processed, total) =>
    {
        ...
    });
```

## RestResult classification

| Value | Meaning |
|-------|---------|
| `Success` | HTTP 2xx and deserialization (if applicable) succeeded |
| `Cancel` | The request was cancelled via `CancellationToken` |
| `RequestError` | A network-level error occurred before a response was received (`HttpRequestException`) |
| `HttpError` | A non-success HTTP status code was returned (4xx / 5xx) |
| `SerializeError` | Deserialization of the response body failed (only on 2xx responses) |
| `Unknown` | An unexpected exception that does not fit any other category |
| `Timeout` | `HttpClient.Timeout` was exceeded (distinct from user-triggered cancellation) |

## NuGet

| Id                                 | Description         |
|------------------------------------|---------------------|
| Rester                             | Rester core         |

## Functions

### Config

Rester config.

```csharp
public sealed class RestConfig
{
    // Shared default configuration
    public static RestConfig Default { get; }

    // Serializer for request/response body
    public ISerializer Serializer { get; set; }

    // Download/Upload buffer size (must be 1 or greater, default: 16 KiB)
    public int TransferBufferSize { get; set; }

    // Serialize POST content directly to the request stream (default: true).
    // When true, the request is sent without Content-Length (chunked transfer encoding).
    // Set false to buffer the serialized content in memory and send with Content-Length.
    public bool PostContentStreaming { get; set; }

    // Handler to resolve content length when the response has no Content-Length (for download progress)
    public Func<ILengthResolveContext, long?> LengthResolver { get; set; }

    // Default Content-Type for upload
    public string DefaultUploadContentType { get; set; }
}
```

Configure `RestConfig.Default` once at application startup and do not change it afterwards. The instance is shared and not synchronized, so changing it while requests are running can cause inconsistent behavior.

Serializer config (System.Text.Json).

```csharp
public static RestConfig UseJsonSerializer(this RestConfig config);

public static RestConfig UseJsonSerializer(this RestConfig config, Action<JsonSerializerOptions> action);

public static RestConfig UseJsonSerializer(this RestConfig config, JsonSerializerContext context, string contentType = "application/json");
```

Serializer config default.

```csharp
public sealed class JsonSerializerConfig
{
    public string ContentType { get; set; } = "application/json";

    public JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
```

### Get

```csharp
public static ValueTask<IRestResponse<T>> GetAsync<T>(
    this HttpClient client,
    string path,
    IDictionary<string, object>? headers = null,
    CancellationToken cancel = default);

// Source-generated JsonTypeInfo overload (Native AOT / trimming friendly)
public static ValueTask<IRestResponse<T>> GetAsync<T>(
    this HttpClient client,
    string path,
    JsonTypeInfo<T> typeInfo,
    IDictionary<string, object>? headers = null,
    CancellationToken cancel = default);
```

Each method also has an overload that takes an explicit `RestConfig config` as the second argument.

### Post

```csharp
public static ValueTask<IRestResponse> PostAsync(
    this HttpClient client,
    string path,
    object parameter,
    IDictionary<string, object>? headers = null,
    CompressOption compress = CompressOption.None,
    CancellationToken cancel = default);

public static ValueTask<IRestResponse<T>> PostAsync<T>(
    this HttpClient client,
    string path,
    object parameter,
    IDictionary<string, object>? headers = null,
    CompressOption compress = CompressOption.None,
    CancellationToken cancel = default);

// Source-generated JsonTypeInfo overloads (Native AOT / trimming friendly)
public static ValueTask<IRestResponse> PostAsync<TRequest>(
    this HttpClient client,
    string path,
    TRequest parameter,
    JsonTypeInfo<TRequest> requestTypeInfo,
    IDictionary<string, object>? headers = null,
    CompressOption compress = CompressOption.None,
    CancellationToken cancel = default);

public static ValueTask<IRestResponse<TResponse>> PostAsync<TRequest, TResponse>(
    this HttpClient client,
    string path,
    TRequest parameter,
    JsonTypeInfo<TRequest> requestTypeInfo,
    JsonTypeInfo<TResponse> responseTypeInfo,
    IDictionary<string, object>? headers = null,
    CompressOption compress = CompressOption.None,
    CancellationToken cancel = default);
```

Each method also has an overload that takes an explicit `RestConfig config` as the second argument.

### Download

```csharp
public static ValueTask<IRestResponse> DownloadAsync(
    this HttpClient client,
    string path,
    string filename,
    IDictionary<string, object>? headers = null,
    Action<long, long>? progress = null,
    CancellationToken cancel = default);

public static ValueTask<IRestResponse> DownloadAsync(
    this HttpClient client,
    string path,
    Stream stream,
    IDictionary<string, object>? headers = null,
    Action<long, long>? progress = null,
    CancellationToken cancel = default);
```

Each method also has an overload that takes an explicit `RestConfig config` as the second argument.

### Upload

Streams passed by the caller (`UploadAsync(Stream)` and `MultipartUploadEntry.Stream`) are NOT disposed by the library.
The caller owns these streams and is responsible for disposing them. Filename overloads open and dispose their own `FileStream` internally.

```csharp
public static ValueTask<IRestResponse> UploadAsync(
    this HttpClient client,
    string path,
    string filename,
    IDictionary<string, object>? headers = null,
    string? contentType = null,
    CompressOption compress = CompressOption.None,
    Action<long, long>? progress = null,
    CancellationToken cancel = default);

public static ValueTask<IRestResponse> UploadAsync(
    this HttpClient client,
    string path,
    Stream stream,
    IDictionary<string, object>? headers = null,
    string? contentType = null,
    CompressOption compress = CompressOption.None,
    Action<long, long>? progress = null,
    CancellationToken cancel = default);

public static ValueTask<IRestResponse> MultipartUploadAsync(
    this HttpClient client,
    string path,
    Stream stream,
    string name,
    string filename,
    IDictionary<string, object>? parameters = null,
    IDictionary<string, object>? headers = null,
    CompressOption compress = CompressOption.None,
    Action<long, long>? progress = null,
    CancellationToken cancel = default);

public static ValueTask<IRestResponse> MultipartUploadAsync(
    this HttpClient client,
    string path,
    IList<MultipartUploadEntry> entries,
    IDictionary<string, object>? parameters = null,
    IDictionary<string, object>? headers = null,
    Action<long, long>? progress = null,
    CancellationToken cancel = default);
```

Each method also has an overload that takes an explicit `RestConfig config` as the second argument.

### Custom serializer

```csharp
public interface ISerializer
{
    string ContentType { get; }

    ValueTask SerializeAsync<T>(Stream stream, T obj, CancellationToken cancel);

    ValueTask SerializeAsync<T>(Stream stream, T obj, JsonTypeInfo<T> typeInfo, CancellationToken cancel);

    ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancel);

    ValueTask<T?> DeserializeAsync<T>(Stream stream, JsonTypeInfo<T> typeInfo, CancellationToken cancel);
}
```
