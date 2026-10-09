namespace Lattice.Core;
/// <summary>A citation into the workspace supporting part of an answer.</summary>
public sealed record EvidenceReference
{
    public EvidenceReference(string relativePath, int startLine, int endLine, string snippet)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentOutOfRangeException.ThrowIfLessThan(startLine, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(endLine, startLine);
        ArgumentNullException.ThrowIfNull(snippet);
        RelativePath = relativePath;
        StartLine = startLine;
        EndLine = endLine;
        Snippet = snippet;
    }
    public string RelativePath { get; }
    public int StartLine { get; }
    public int EndLine { get; }
    public string Snippet { get; }
    public override string ToString() =>
        StartLine == EndLine
            ? $"{RelativePath}:{StartLine}"
            : $"{RelativePath}:{StartLine}-{EndLine}";
}