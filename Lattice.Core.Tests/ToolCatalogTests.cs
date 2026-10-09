namespace Lattice.Core.Tests;
public sealed class ToolCatalogTests
{
    [Fact]
    public void RejectsDuplicateIds()
    {
        var descriptors = new[] { Descriptor("calculator"), Descriptor("calculator") };
        Assert.Throws<ArgumentException>(() => new ToolCatalog(descriptors));
    }
    [Fact]
    public void AcceptsDistinctIds()
    {
        var catalog = new ToolCatalog(new[] { Descriptor("calculator"), Descriptor("clock") });
        Assert.Equal(2, catalog.Descriptors.Length);
    }
    [Fact]
    public void CatalogsWithSameDescriptorsAreEqual()
    {
        var first = new ToolCatalog(new[] { Descriptor("calculator") });
        var second = new ToolCatalog(new[] { Descriptor("calculator") });
        Assert.Equal(first, second);
    }
    private static ToolDescriptor Descriptor(string id) =>
        new(new ToolId(id), "desc", "out", ToolSideEffect.ReadOnly);
}