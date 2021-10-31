namespace Rester.Transfer
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class MultipartUploadEntry
    {
        public Stream Stream { get; }

        public string Name { get; }

        public string FileName { get; }

        public Func<Stream, Stream, Func<Stream, Stream, ValueTask>, ValueTask>? Filter { get; set; }

        public MultipartUploadEntry(Stream stream, string name, string fileName)
        {
            Stream = stream;
            Name = name;
            FileName = fileName;
        }
    }
}
