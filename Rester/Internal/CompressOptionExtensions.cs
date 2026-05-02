namespace Rester.Internal;

internal static class CompressOptionExtensions
{
    internal static string ToContentEncoding(this CompressOption compress) => compress switch
    {
        CompressOption.Gzip => "gzip",
        CompressOption.Deflate => "deflate",
        _ => string.Empty
    };
}
