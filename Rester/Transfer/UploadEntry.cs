namespace Rester.Transfer
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class UploadEntry
    {
        public Stream Stream { get; }

        public string Name { get; }

        public string FileName { get; }

        public Func<Stream, Stream, Func<Stream, Stream, Task>, Task> Filter { get; set; }

        public UploadEntry(Stream stream, string name, string fileName)
        {
            Stream = stream;
            Name = name;
            FileName = fileName;
        }
    }
}
