# Rester - HttpClient extensions for rest api

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
var response = await client.PostAsync("api/test/post", new TestPostRequest { Data = "..." }, compress: true);

// Download with progress
var response = await client.DownloadAsync(
    "api/test/download/test.dat",
    "test.dat",
    progress: (processed, total) =>
    {
        ...
    });

// Multiple file upload with other parameter and progress
var response = await client.UploadAsync(
    "api/test/upload",
    new List<UploadEntry>
    {
        new UploadEntry(stream1, "file1", "test.txt"),
        new UploadEntry(stream2, "file2", "test.csv").WithGzip()
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

## NuGet

| Id                                 | Description         |
|------------------------------------|---------------------|
| Rester                             | Rester core         |
| Rester.Serializers.Newtonsoft.Json | Json.NET serializer |

## Functions

### Config

Rester config.

```csharp
public class RestConfig
{
    // Serializer for object
    public ISerializer Serializer { get; set; }

    // Default UTF-8
    public Encoding Encoding { get; set; }

    // Content-Encoding
    public ContentEncoding ContentEncoding { get; set; }

    // Download/Upload buffer size
    public int TransferBufferSize { get; set; }

    // Download content length missing handler
    public Func<ILengthResolveContext, long?> LengthResolver { get; set; }
}
```

Serializer config for Json.NET.

```csharp
public static RestConfig UseJsonSerializer(this RestConfig config);

public static RestConfig UseJsonSerializer(this RestConfig config, Action<JsonSerializerConfig> action);
```

Json.NET config default.

```csharp
public class JsonSerializerConfig
{
    public string ContentType { get; set; } = "application/json";

    public JsonSerializerSettings Settings { get; } = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        DateFormatHandling = DateFormatHandling.IsoDateFormat
    };
}
```

### Get

```csharp
public static Task<IRestResponse<T>> GetAsync<T>(
    this HttpClient client,
    string path,
    IDictionary<string, object> headers = null,
    CancellationToken cancel = default);

public static async Task<IRestResponse<T>> GetAsync<T>(
    this HttpClient client,
    RestConfig config,
    string path,
    IDictionary<string, object> headers = null,
    CancellationToken cancel = default);
```

### Post

```csharp
public static Task<IRestResponse> PostAsync(
    this HttpClient client,
    string path,
    object parameter,
    IDictionary<string, object> headers = null,
    bool compress = false,
    CancellationToken cancel = default);

public static async Task<IRestResponse> PostAsync(
    this HttpClient client,
    RestConfig config,
    string path,
    object parameter,
    IDictionary<string, object> headers = null,
    bool compress = false,
    CancellationToken cancel = default);
```

### Download

```csharp
public static Task<IRestResponse> DownloadAsync(
    this HttpClient client,
    string path,
    string filename,
    IDictionary<string, object> headers = null,
    Action<long, long> progress = null,
    CancellationToken cancel = default);

public static async Task<IRestResponse> DownloadAsync(
    this HttpClient client,
    RestConfig config,
    string path,
    string filename,
    IDictionary<string, object> headers = null,
    Action<long, long> progress = null,
    CancellationToken cancel = default);

public static Task<IRestResponse> DownloadAsync(
    this HttpClient client,
    string path,
    Stream stream,
    IDictionary<string, object> headers = null,
    Action<long, long> progress = null,
    CancellationToken cancel = default);

public static async Task<IRestResponse> DownloadAsync(
    this HttpClient client,
    RestConfig config,
    string path,
    Stream stream,
    IDictionary<string, object> headers = null,
    Action<long, long> progress = null,
    CancellationToken cancel = default);
```

### Upload

```csharp
public static Task<IRestResponse> UploadAsync(
    this HttpClient client,
    string path,
    Stream stream,
    string name,
    string filename,
    Func<Stream, Stream, Func<Stream, Stream, Task>, Task> filter = null,
    IDictionary<string, object> parameters = null,
    IDictionary<string, object> headers = null,
    Action<long, long> progress = null,
    CancellationToken cancel = default);

public static Task<IRestResponse> UploadAsync(
    this HttpClient client,
    RestConfig config,
    string path,
    Stream stream,
    string name,
    string filename,
    Func<Stream, Stream, Func<Stream, Stream, Task>, Task> filter = null,
    IDictionary<string, object> parameters = null,
    IDictionary<string, object> headers = null,
    Action<long, long> progress = null,
    CancellationToken cancel = default);

public static Task<IRestResponse> UploadAsync(
    this HttpClient client,
    string path,
    string name,
    string filename,
    Func<Stream, Stream, Func<Stream, Stream, Task>, Task> filter = null,
    IDictionary<string, object> parameters = null,
    IDictionary<string, object> headers = null,
    Action<long, long> progress = null,
    CancellationToken cancel = default);

public static async Task<IRestResponse> UploadAsync(
    this HttpClient client,
    RestConfig config,
    string path,
    string name,
    string filename,
    Func<Stream, Stream, Func<Stream, Stream, Task>, Task> filter = null,
    IDictionary<string, object> parameters = null,
    IDictionary<string, object> headers = null,
    Action<long, long> progress = null,
    CancellationToken cancel = default);

public static Task<IRestResponse> UploadAsync(
    this HttpClient client,
    string path,
    IList<UploadEntry> entries,
    IDictionary<string, object> parameters = null,
    IDictionary<string, object> headers = null,
    Action<long, long> progress = null,
    CancellationToken cancel = default);

public static async Task<IRestResponse> UploadAsync(
    this HttpClient client,
    RestConfig config,
    string path,
    IList<UploadEntry> entries,
    IDictionary<string, object> parameters = null,
    IDictionary<string, object> headers = null,
    Action<long, long> progress = null,
    CancellationToken cancel = default);
```

### Custom serializer

```csharp
public interface ISerializer
{
    string ContentType { get; }

    string Serialize(object obj);

    T Deserialize<T>(string json);
}
```
