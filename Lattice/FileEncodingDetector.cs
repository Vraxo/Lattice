using System.Text;
namespace Lattice.Core;
/// <summary>
/// Classifies a file's encoding from its raw bytes. Detection is explicit rather than delegated to
/// a text reader's implicit byte-order-mark handling, because that implicit handling is exactly what
/// causes a file to be read in one encoding and silently rewritten in another.
/// </summary>
internal static class FileEncodingDetector
{
    // Byte-order marks. Stored as byte[] so StartsWith binds the byte overload rather than the int
    // one (a bare collection expression infers int[]).
    private static readonly byte[] Utf32BeBom = [0x00, 0x00, 0xFE, 0xFF];
    private static readonly byte[] Utf32LeBom = [0xFF, 0xFE, 0x00, 0x00];
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] Utf16BeBom = [0xFE, 0xFF];
    private static readonly byte[] Utf16LeBom = [0xFF, 0xFE];
    private static readonly UTF8Encoding StrictUtf8 =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    /// <summary>
    /// Detects the encoding of <paramref name="bytes"/>. Byte-order marks are checked longest and
    /// most specific first: UTF-32 LE's mark (FF FE 00 00) shares its first two bytes with UTF-16
    /// LE's (FF FE), so it must be tested before UTF-16 LE. Bytes with no mark are classified by
    /// strict UTF-8 validation.
    /// </summary>
    public static FileEncodingKind Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(Utf32BeBom))
        {
            return FileEncodingKind.Utf32Be;
        }
        if (bytes.StartsWith(Utf32LeBom))
        {
            return FileEncodingKind.Utf32Le;
        }
        if (bytes.StartsWith(Utf8Bom))
        {
            return FileEncodingKind.Utf8WithBom;
        }
        if (bytes.StartsWith(Utf16BeBom))
        {
            return FileEncodingKind.Utf16Be;
        }
        if (bytes.StartsWith(Utf16LeBom))
        {
            return FileEncodingKind.Utf16Le;
        }
        try
        {
            StrictUtf8.GetString(bytes);
            return FileEncodingKind.Utf8NoBom;
        }
        catch (DecoderFallbackException)
        {
            return FileEncodingKind.Unknown;
        }
    }
}