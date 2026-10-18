namespace Lattice.Core;

/// <summary>
/// The kind of transformation an edit instruction requests. Only some kinds are executable by the
/// current editor; see <see cref="EditOperationSupport"/>.
/// </summary>
public enum EditOperationKind
{
    /// <summary>Replace the content of a named target section. Executable.</summary>
    ReplaceSection,

    /// <summary>Shorten the target content. Representable, not executable.</summary>
    Shorten,

    /// <summary>Change the tone of the target content. Representable, not executable.</summary>
    ChangeTone,
}