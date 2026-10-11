namespace Lattice.Core;

/// <summary>
/// The encoding a file's bytes were detected to use. Only <see cref="Utf8NoBom"/> can currently be
/// edited without risking a silent change to the file's encoding or byte-order mark; the other
/// values exist so an edit can be refused rather than applied destructively.
/// </summary>
internal enum FileEncodingKind
{
    /// <summary>UTF-8 with no byte-order mark. Editable.</summary>
    Utf8NoBom,

    /// <summary>UTF-8 with a byte-order mark. Editing is refused until the mark can be preserved.</summary>
    Utf8WithBom,

    /// <summary>UTF-16 little-endian (with a byte-order mark). Editing is refused.</summary>
    Utf16Le,

    /// <summary>UTF-16 big-endian (with a byte-order mark). Editing is refused.</summary>
    Utf16Be,

    /// <summary>UTF-32 little-endian (with a byte-order mark). Editing is refused.</summary>
    Utf32Le,

    /// <summary>UTF-32 big-endian (with a byte-order mark). Editing is refused.</summary>
    Utf32Be,

    /// <summary>Not recognised as text in a supported encoding. Never edited.</summary>
    Unknown,
}