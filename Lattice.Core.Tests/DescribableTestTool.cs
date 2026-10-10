namespace Lattice.Core.Tests;

/// <summary>
/// A test capability that is discoverable purely through its descriptor metadata. Nothing in
/// Core knows this tool exists; registration alone makes it findable.
/// </summary>
internal sealed class DescribableTestTool(string id, string[]? tags = null, string[]? aliases = null) : ITool
{
    public ToolDescriptor Descriptor { get; } = new ToolDescriptor(
            new ToolId(id),
            "A test capability used to prove descriptor-driven discovery.",
            "A boolean.",
            ToolSideEffect.ReadOnly,
            aliases: aliases,
            tags: tags);

    public ToolResult Execute(ArgumentBag arguments)
    {
        return ToolResult.Success(ArgumentValue.FromBoolean(true));
    }
}