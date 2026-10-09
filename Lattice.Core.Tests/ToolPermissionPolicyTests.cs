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
        ToolPermissionPolicy policy = ToolPermissionPolicy.AllowAll;
        Assert.True(policy.IsAllowed(ToolSideEffect.ReadOnly));
        Assert.True(policy.IsAllowed(ToolSideEffect.LocalWrite));
        Assert.True(policy.IsAllowed(ToolSideEffect.CommandExecution));
        Assert.True(policy.IsAllowed(ToolSideEffect.ExternalSideEffect));
    }

    [Fact]
    public void GrantAllowsOnlyListedSideEffects()
    {
        ToolPermissionPolicy policy = ToolPermissionPolicy.Grant([ToolSideEffect.LocalWrite]);
        Assert.True(policy.IsAllowed(ToolSideEffect.LocalWrite));
        Assert.False(policy.IsAllowed(ToolSideEffect.ReadOnly));
        Assert.False(policy.IsAllowed(ToolSideEffect.ExternalSideEffect));
    }

    [Fact]
    public void PoliciesWithSameGrantsAreEqual()
    {
        ToolPermissionPolicy first = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly, ToolSideEffect.LocalWrite]);
        ToolPermissionPolicy second = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly, ToolSideEffect.LocalWrite]);
        Assert.Equal(first, second);
    }

    [Fact]
    public void PoliciesWithDifferentGrantsAreNotEqual()
    {
        ToolPermissionPolicy first = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly]);
        ToolPermissionPolicy second = ToolPermissionPolicy.Grant([ToolSideEffect.LocalWrite]);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GrantIsOrderIndependent()
    {
        ToolPermissionPolicy first = ToolPermissionPolicy.Grant([ToolSideEffect.LocalWrite, ToolSideEffect.ReadOnly]);
        ToolPermissionPolicy second = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly, ToolSideEffect.LocalWrite]);
        Assert.Equal(first, second);
    }

    [Fact]
    public void GrantDeduplicates()
    {
        ToolPermissionPolicy duplicated = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly, ToolSideEffect.ReadOnly]);
        ToolPermissionPolicy single = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly]);
        Assert.Equal(duplicated, single);
    }
}