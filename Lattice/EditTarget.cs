namespace Lattice.Core;
/// <summary>
/// What an edit instruction applies to, independent of document format. <see cref="Kind"/> says how
/// to interpret <see cref="Reference"/>; format-specific details (for example a Markdown heading
/// level) belong in <see cref="EditInstruction.Parameters"/>, not here.
/// </summary>
public sealed record EditTarget
{
    private EditTarget(EditTargetKind kind, string reference)
    {
        Kind = kind;
        Reference = reference;
    }

    public EditTargetKind Kind { get; }

    public string Reference { get; }

    public static EditTarget Section(string heading)
    {
        return Create(EditTargetKind.Section, heading);
    }

    public static EditTarget Path(string relativePath)
    {
        return Create(EditTargetKind.Path, relativePath);
    }

    public static EditTarget Document(string relativePath)
    {
        return Create(EditTargetKind.Document, relativePath);
    }

    public override string ToString()
    {
        return $"{Kind}:{Reference}";
    }

    private static EditTarget Create(EditTargetKind kind, string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        return new EditTarget(kind, reference);
    }
}