using System.Text;

namespace Lattice.Core;

/// <summary>
/// Classifies a file's encoding from its raw bytes. Detection is explicit rather than delegated to
/// a text reader's implicit byte-order-mark handling, because that implicit handling is exactly what
/// causes a file to be read in one encoding and silently rewritten in another.
/// </summary>
internal static class FileEncodingDetector
{
    private static readonly UTF8Encoding StrictUtf8 =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Detects the encoding of <paramref name="bytes"/>. Byte-order marks are checked longest and
    /// most specific first: UTF-32 LE's mark (FF FE 00 00) shares its first two bytes with UTF-16
    /// LE's (FF FE), so it must be tested before UTF-16 LE. Bytes with no mark are classified by
    /// strict UTF-8 validation.
    /// </summary>
    /// <returns></returns>
    public static FileEncodingKind Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith([0x00, 0x00, 0xFE, 0xFF]))
        {
            return FileEncodingKind.Utf32Be;
        }

        if (bytes.StartsWith([0xFF, 0xFE, 0x00, 0x00]))
        {
            return FileEncodingKind.Utf32Le;
        }

        if (bytes.StartsWith([0xEF, 0xBB, 0xBF]))
        {
            return FileEncodingKind.Utf8WithBom;
        }

        if (bytes.StartsWith([0xFE, 0xFF]))
        {
            return FileEncodingKind.Utf16Be;
        }

        if (bytes.StartsWith([0xFF, 0xFE]))
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