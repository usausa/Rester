namespace Rester
{
    using System;

    using Rester.Serializers;
    using Rester.Transfer;

    public class RestConfig
    {
        public static RestConfig Default { get; } = new();

        public ISerializer Serializer { get; set; }

        public ContentEncoding ContentEncoding { get; set; } = ContentEncoding.Gzip;

        public int TransferBufferSize { get; set; } = 16 * 1024;

        public Func<ILengthResolveContext, long?> LengthResolver { get; set; }
    }
}
