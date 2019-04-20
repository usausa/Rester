namespace Rester.Transfer
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Threading.Tasks;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2008:DoNotCreateTasksWithoutPassingATaskScheduler", Justification = "Ignore")]
    public static class UploadEntryExtensions
    {
        private static readonly Func<Stream, Stream, Func<Stream, Stream, Task>, Task> GzipFilter = (source, destination, task) =>
        {
            var compressedStream = (Stream)new GZipStream(destination, CompressionMode.Compress, true);
            return task(source, compressedStream).ContinueWith(t => compressedStream.Dispose());
        };

        private static readonly Func<Stream, Stream, Func<Stream, Stream, Task>, Task> DeflateFilter = (source, destination, task) =>
        {
            var compressedStream = (Stream)new DeflateStream(destination, CompressionMode.Compress, true);
            return task(source, compressedStream).ContinueWith(t => compressedStream.Dispose());
        };

        public static UploadEntry WithGzip(this UploadEntry entry)
        {
            entry.Filter = GzipFilter;
            return entry;
        }

        public static UploadEntry WithDeflate(this UploadEntry entry)
        {
            entry.Filter = DeflateFilter;
            return entry;
        }
    }
}
