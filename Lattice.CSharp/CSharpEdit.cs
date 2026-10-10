namespace Lattice.CSharp;
/// <summary>
/// The result of a syntax-aware edit. <see cref="PatchedText"/> is non-null only when the
/// status is <see cref="CSharpEditStatus.Applied"/>.
/// </summary>
public sealed record CSharpEdit
{
    private CSharpEdit(CSharpEditStatus status, string original, string? patchedText, string message)
    {
        Status = status;
        Original = original;
        PatchedText = patchedText;
        Message = message;
    }

    public CSharpEditStatus Status { get; }

    public string Original { get; }

    public string? PatchedText { get; }

    public string Message { get; }

    public bool IsApplied => Status == CSharpEditStatus.Applied;

    public static CSharpEdit Applied(string original, string patchedText, string message)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(patchedText);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new CSharpEdit(CSharpEditStatus.Applied, original, patchedText, message);
    }

    public static CSharpEdit Rejected(CSharpEditStatus status, string original, string message)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (status == CSharpEditStatus.Applied)
        {
            throw new ArgumentException("Use Applied for a successful edit.", nameof(status));
        }

        return new CSharpEdit(status, original, null, message);
    }
}