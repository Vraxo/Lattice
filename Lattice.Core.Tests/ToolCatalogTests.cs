namespace Lattice.Core.Tests;

public sealed class ToolCatalogTests
{
    [Fact]
    public void RejectsDuplicateIds()
    {
        ToolDescriptor[] descriptors = [Descriptor("calculator"), Descriptor("calculator")];
        Assert.Throws<ArgumentException>(() => new ToolCatalog(descriptors));
    }

    [Fact]
    public void AcceptsDistinctIds()
    {
        ToolCatalog catalog = new([Descriptor("calculator"), Descriptor("clock")]);
        Assert.Equal(2, catalog.Descriptors.Length);
    }

    [Fact]
    public void CatalogsWithSameDescriptorsAreEqual()
    {
        ToolCatalog first = new([Descriptor("calculator")]);
        ToolCatalog second = new([Descriptor("calculator")]);
        Assert.Equal(first, second);
    }

    [Fact]
    public void ResolvesRegisteredTool()
    {
        ToolCatalog catalog = new([Descriptor("calculator")]);
        Assert.True(catalog.TryResolve(new ToolId("calculator"), out ToolDescriptor? descriptor));
        Assert.Equal(new ToolId("calculator"), descriptor.Id);
    }

    [Fact]
    public void UnknownToolFailsToResolve()
    {
        ToolCatalog catalog = new([Descriptor("calculator")]);
        Assert.False(catalog.TryResolve(new ToolId("clock"), out _));
    }

    private static ToolDescriptor Descriptor(string id)
    {
        return new(new ToolId(id), "desc", "out", ToolSideEffect.ReadOnly);
    }
}