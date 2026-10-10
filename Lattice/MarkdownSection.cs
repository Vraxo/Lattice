namespace Lattice.Core;
/// <summary>
/// A heading together with the range of lines it governs. <see cref="ContentRange"/> covers the
/// lines after the heading up to, but not including, the next heading of the same or higher level
/// (or the end of the document). The heading line itself is excluded, so a section-content edit
/// preserves the heading unless deletion is requested explicitly.
/// </summary>
public sealed record MarkdownSection
{
    public MarkdownSection(MarkdownHeading heading, LineRange contentRange)
    {
        ArgumentNullException.ThrowIfNull(heading);
        Heading = heading;
        ContentRange = contentRange;
    }

    public MarkdownHeading Heading { get; }

    /// <summary>Gets lines governed by the heading, excluding the heading line itself.</summary>
    public LineRange ContentRange { get; }

    /// <summary>Gets the heading line together with the content it governs.</summary>
    public LineRange FullRange => new(Heading.LineIndex, ContentRange.End);

    public override string ToString()
    {
        return $"{Heading} -> content {ContentRange}";
    }
}