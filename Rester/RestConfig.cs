namespace Rester
{
    using System;
    using System.Text;

    using Rester.Serializers;
    using Rester.Transfer;

    public class RestConfig
    {
        public static RestConfig Default { get; } = new RestConfig();

        public ISerializer Serializer { get; set; }

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public EncodingType PostEncodingType { get; set; } = EncodingType.Gzip;

        public int TransferBufferSize { get; set; } = 16 * 1024;

        public Func<ILengthResolveContext, long?> LengthResolver { get; set; }
    }
}
