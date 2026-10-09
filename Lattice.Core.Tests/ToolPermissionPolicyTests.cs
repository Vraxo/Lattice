namespace Lattice.Core.Tests;
public sealed class ToolPermissionPolicyTests
{
    [Fact]
    public void ReadOnlyOnlyAllowsReadOnly()
    {
        Assert.True(ToolPermissionPolicy.ReadOnlyOnly.IsAllowed(ToolSideEffect.ReadOnly));
    }
    [Fact]
    public void ReadOnlyOnlyDeniesLocalWrite()
    {
        Assert.False(ToolPermissionPolicy.ReadOnlyOnly.IsAllowed(ToolSideEffect.LocalWrite));
    }
    [Fact]
    public void ReadOnlyOnlyDeniesCommandExecution()
    {
        Assert.False(ToolPermissionPolicy.ReadOnlyOnly.IsAllowed(ToolSideEffect.CommandExecution));
    }
    [Fact]
    public void AllowAllAllowsEverySideEffect()
    {
        var policy = ToolPermissionPolicy.AllowAll;
        Assert.True(policy.IsAllowed(ToolSideEffect.ReadOnly));
        Assert.True(policy.IsAllowed(ToolSideEffect.LocalWrite));
        Assert.True(policy.IsAllowed(ToolSideEffect.CommandExecution));
        Assert.True(policy.IsAllowed(ToolSideEffect.ExternalSideEffect));
    }
    [Fact]
    public void GrantAllowsOnlyListedSideEffects()
    {
        var policy = ToolPermissionPolicy.Grant(new[] { ToolSideEffect.LocalWrite });
        Assert.True(policy.IsAllowed(ToolSideEffect.LocalWrite));
        Assert.False(policy.IsAllowed(ToolSideEffect.ReadOnly));
        Assert.False(policy.IsAllowed(ToolSideEffect.ExternalSideEffect));
    }
    [Fact]
    public void PoliciesWithSameGrantsAreEqual()
    {
        var first = ToolPermissionPolicy.Grant(new[] { ToolSideEffect.ReadOnly, ToolSideEffect.LocalWrite });
        var second = ToolPermissionPolicy.Grant(new[] { ToolSideEffect.ReadOnly, ToolSideEffect.LocalWrite });
        Assert.Equal(first, second);
    }
    [Fact]
    public void PoliciesWithDifferentGrantsAreNotEqual()
    {
        var first = ToolPermissionPolicy.Grant(new[] { ToolSideEffect.ReadOnly });
        var second = ToolPermissionPolicy.Grant(new[] { ToolSideEffect.LocalWrite });
        Assert.NotEqual(first, second);
    }
    [Fact]
    public void GrantIsOrderIndependent()
    {
        var first = ToolPermissionPolicy.Grant(new[] { ToolSideEffect.LocalWrite, ToolSideEffect.ReadOnly });
        var second = ToolPermissionPolicy.Grant(new[] { ToolSideEffect.ReadOnly, ToolSideEffect.LocalWrite });
        Assert.Equal(first, second);
    }
    [Fact]
    public void GrantDeduplicates()
    {
        var duplicated = ToolPermissionPolicy.Grant(new[] { ToolSideEffect.ReadOnly, ToolSideEffect.ReadOnly });
        var single = ToolPermissionPolicy.Grant(new[] { ToolSideEffect.ReadOnly });
        Assert.Equal(duplicated, single);
    }
}