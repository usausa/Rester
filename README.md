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

// Put
var response = await client.PutAsync("api/test/put/123", new TestPutRequest { Data = "..." });

// Patch
var response = await client.PatchAsync("api/test/patch/123", new TestPatchRequest { Data = "..." });

// Delete
var response = await client.DeleteAsync<TestDeleteResponse>("api/test/delete/123");

// Delete without reading the response body (HttpClient.DeleteAsync hides a non-generic extension)
var response = await client.SendAsync(HttpMethod.Delete, "api/test/delete/123");

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

## Response

```csharp
public interface IRestResponse
{
    RestResult RestResult { get; }

    HttpStatusCode StatusCode { get; }

    // Response headers (Location, ETag, ...). null when no response was received.
    HttpResponseHeaders? Headers { get; }

    Exception? InnerException { get; }
}

public interface IRestResponse<out T> : IRestResponse
{
    T? Content { get; }
}
```

`Headers` is set whenever a response was received, including `HttpError` and `SerializeError`. The `HttpResponseMessage` itself is disposed before the method returns, so the response body is not accessible.

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

    // Serialize POST/PUT content directly to the request stream (default: true).
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

### Put

Same overloads as Post, sent with the PUT method.

```csharp
public static ValueTask<IRestResponse> PutAsync(
    this HttpClient client,
    string path,
    object parameter,
    IDictionary<string, object>? headers = null,
    CompressOption compress = CompressOption.None,
    CancellationToken cancel = default);

public static ValueTask<IRestResponse<T>> PutAsync<T>(
    this HttpClient client,
    string path,
    object parameter,
    IDictionary<string, object>? headers = null,
    CompressOption compress = CompressOption.None,
    CancellationToken cancel = default);

// Source-generated JsonTypeInfo overloads (Native AOT / trimming friendly)
public static ValueTask<IRestResponse> PutAsync<TRequest>(
    this HttpClient client,
    string path,
    TRequest parameter,
    JsonTypeInfo<TRequest> requestTypeInfo,
    IDictionary<string, object>? headers = null,
    CompressOption compress = CompressOption.None,
    CancellationToken cancel = default);

public static ValueTask<IRestResponse<TResponse>> PutAsync<TRequest, TResponse>(
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

### Patch

Same overloads as Post, sent with the PATCH method.

```csharp
public static ValueTask<IRestResponse> PatchAsync(
    this HttpClient client,
    string path,
    object parameter,
    IDictionary<string, object>? headers = null,
    CompressOption compress = CompressOption.None,
    CancellationToken cancel = default);

public static ValueTask<IRestResponse<T>> PatchAsync<T>(
    this HttpClient client,
    string path,
    object parameter,
    IDictionary<string, object>? headers = null,
    CompressOption compress = CompressOption.None,
    CancellationToken cancel = default);

// Source-generated JsonTypeInfo overloads (Native AOT / trimming friendly)
public static ValueTask<IRestResponse> PatchAsync<TRequest>(
    this HttpClient client,
    string path,
    TRequest parameter,
    JsonTypeInfo<TRequest> requestTypeInfo,
    IDictionary<string, object>? headers = null,
    CompressOption compress = CompressOption.None,
    CancellationToken cancel = default);

public static ValueTask<IRestResponse<TResponse>> PatchAsync<TRequest, TResponse>(
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

### Delete

No request body is sent, the JSON response body is deserialized like Get.

```csharp
public static ValueTask<IRestResponse<T>> DeleteAsync<T>(
    this HttpClient client,
    string path,
    IDictionary<string, object>? headers = null,
    CancellationToken cancel = default);

// Source-generated JsonTypeInfo overload (Native AOT / trimming friendly)
public static ValueTask<IRestResponse<T>> DeleteAsync<T>(
    this HttpClient client,
    string path,
    JsonTypeInfo<T> typeInfo,
    IDictionary<string, object>? headers = null,
    CancellationToken cancel = default);
```

Each method also has an overload that takes an explicit `RestConfig config` as the second argument.

There is no non-generic `DeleteAsync(path)`: `HttpClient` itself defines `DeleteAsync(string)` and `DeleteAsync(string, CancellationToken)`,
and instance methods always take precedence over extension methods, so such an overload could never be called. Use `SendAsync(HttpMethod.Delete, path)` instead.

### Send

Sends a request with the specified method, without request body, and without reading the response body.
Use it for DELETE endpoints that return `204 No Content`, body-less POST actions, HEAD, etc.

```csharp
public static ValueTask<IRestResponse> SendAsync(
    this HttpClient client,
    HttpMethod method,
    string path,
    IDictionary<string, object>? headers = null,
    CancellationToken cancel = default);
```

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
