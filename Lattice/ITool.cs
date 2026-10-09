namespace Lattice.Core;
public interface ITool
{
    ToolDescriptor Descriptor { get; }
    ToolResult Execute(ArgumentBag arguments);
}