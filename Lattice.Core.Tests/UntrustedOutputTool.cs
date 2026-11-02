namespace Lattice.Core.Tests;

internal sealed class UntrustedOutputTool : ITool
{
    private readonly string _content;

    public UntrustedOutputTool(string id, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(content);
        _content = content;
        Descriptor = new ToolDescriptor(
            new ToolId(id),
            "Returns fixed untrusted test text.",
            "The supplied text as a string.",
            ToolSideEffect.ReadOnly);
    }

    public ToolDescriptor Descriptor { get; }

    public ToolResult Execute(ArgumentBag arguments, CancellationToken cancellationToken = default)
    {
        return ToolResult.Success(ArgumentValue.FromString(_content));
    }
}
