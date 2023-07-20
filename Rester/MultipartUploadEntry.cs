namespace Rester;

public class MultipartUploadEntry
{
    public Stream Stream { get; }

    public string Name { get; }

    public string FileName { get; }

    public CompressOption Compress { get; }

    public string? ContentType { get; }

    public MultipartUploadEntry(Stream stream, string name, string fileName, CompressOption compress = CompressOption.None, string? contentType = null)
    {
        Stream = stream;
        Name = name;
        FileName = fileName;
        Compress = compress;
        ContentType = contentType;
    }
}
