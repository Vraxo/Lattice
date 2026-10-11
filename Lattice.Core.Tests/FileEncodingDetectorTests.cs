using System.Text;

namespace Lattice.Core.Tests;

/// <summary>
/// Tests the encoding detector directly. The detector is the single decision point behind the write
/// guard, so its classification is pinned here rather than only observed indirectly through a tool.
/// </summary>
public sealed class FileEncodingDetectorTests
{
    [Fact]
    public void PlainUtf8IsDetectedAsUtf8NoBom()
    {
        Assert.Equal(FileEncodingKind.Utf8NoBom, FileEncodingDetector.Detect(Encoding.UTF8.GetBytes("plain\n")));
    }

    [Fact]
    public void EmptyBytesAreDetectedAsUtf8NoBom()
    {
        Assert.Equal(FileEncodingKind.Utf8NoBom, FileEncodingDetector.Detect([]));
    }

    [Fact]
    public void Utf8BomIsDetected()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("plain\n")];
        Assert.Equal(FileEncodingKind.Utf8WithBom, FileEncodingDetector.Detect(bytes));
    }

    [Fact]
    public void Utf16LeIsDetected()
    {
        byte[] bytes = [0xFF, 0xFE, .. Encoding.Unicode.GetBytes("wide\n")];
        Assert.Equal(FileEncodingKind.Utf16Le, FileEncodingDetector.Detect(bytes));
    }

    [Fact]
    public void Utf16BeIsDetected()
    {
        byte[] bytes = [0xFE, 0xFF, .. Encoding.BigEndianUnicode.GetBytes("wide\n")];
        Assert.Equal(FileEncodingKind.Utf16Be, FileEncodingDetector.Detect(bytes));
    }

    [Fact]
    public void Utf32LeIsDetectedBeforeUtf16LeBecauseTheirMarksOverlap()
    {
        byte[] bytes = [0xFF, 0xFE, 0x00, 0x00, .. Encoding.UTF32.GetBytes("wide\n")];
        Assert.Equal(FileEncodingKind.Utf32Le, FileEncodingDetector.Detect(bytes));
    }

    [Fact]
    public void Utf32BeIsDetected()
    {
        UTF32Encoding utf32Be = new(bigEndian: true, byteOrderMark: false);
        byte[] bytes = [0x00, 0x00, 0xFE, 0xFF, .. utf32Be.GetBytes("wide\n")];
        Assert.Equal(FileEncodingKind.Utf32Be, FileEncodingDetector.Detect(bytes));
    }

    [Fact]
    public void InvalidUtf8IsDetectedAsUnknown()
    {
        Assert.Equal(FileEncodingKind.Unknown, FileEncodingDetector.Detect([0xC3, 0x28]));
    }
}