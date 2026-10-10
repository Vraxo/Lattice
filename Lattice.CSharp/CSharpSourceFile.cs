namespace Lattice.CSharp;
/// <summary>
/// One source input to a semantic inspection. <see cref="Path"/> is used as the syntax tree's
/// path so diagnostics and declared-symbol locations point at a meaningful file.
/// </summary>
public sealed record CSharpSourceFile
{
    public CSharpSourceFile(string path, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(text);
        Path = path;
        Text = text;
    }

    public string Path { get; }

    public string Text { get; }
}