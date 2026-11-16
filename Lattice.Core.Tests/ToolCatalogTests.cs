namespace Lattice.Core.Tests;

public sealed class ToolCatalogTests
{
    [Fact]
    public void RejectsDuplicateIds()
    {
        ToolDescriptor[] descriptors = [Descriptor("calculator"), Descriptor("calculator")];
        FluentActions.Invoking(() => new ToolCatalog(descriptors)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void AcceptsDistinctIds()
    {
        ToolCatalog catalog = new([Descriptor("calculator"), Descriptor("clock")]);
        catalog.Descriptors.Length.Should().Be(2);
    }

    [Fact]
    public void CatalogsWithSameDescriptorsAreEqual()
    {
        ToolCatalog first = new([Descriptor("calculator")]);
        ToolCatalog second = new([Descriptor("calculator")]);
        second.Should().Be(first);
    }

    [Fact]
    public void ResolvesRegisteredTool()
    {
        ToolCatalog catalog = new([Descriptor("calculator")]);
        catalog.TryResolve(new ToolId("calculator"), out ToolDescriptor? descriptor).Should().BeTrue();
        descriptor.Id.Should().Be(new ToolId("calculator"));
    }

    [Fact]
    public void UnknownToolFailsToResolve()
    {
        ToolCatalog catalog = new([Descriptor("calculator")]);
        catalog.TryResolve(new ToolId("clock"), out _).Should().BeFalse();
    }

    private static ToolDescriptor Descriptor(string id)
    {
        return new(new ToolId(id), "desc", "out", ToolSideEffect.ReadOnly);
    }
}