namespace Lattice.Core.Tests;

public sealed class ToolPermissionPolicyTests
{
    [Fact]
    public void ReadOnlyOnlyAllowsReadOnly()
    {
        ToolPermissionPolicy.ReadOnlyOnly.IsAllowed(ToolSideEffect.ReadOnly).Should().BeTrue();
    }

    [Fact]
    public void ReadOnlyOnlyDeniesLocalWrite()
    {
        ToolPermissionPolicy.ReadOnlyOnly.IsAllowed(ToolSideEffect.LocalWrite).Should().BeFalse();
    }

    [Fact]
    public void ReadOnlyOnlyDeniesCommandExecution()
    {
        ToolPermissionPolicy.ReadOnlyOnly.IsAllowed(ToolSideEffect.CommandExecution).Should().BeFalse();
    }

    [Fact]
    public void AllowAllAllowsEverySideEffect()
    {
        ToolPermissionPolicy policy = ToolPermissionPolicy.AllowAll;
        policy.IsAllowed(ToolSideEffect.ReadOnly).Should().BeTrue();
        policy.IsAllowed(ToolSideEffect.LocalWrite).Should().BeTrue();
        policy.IsAllowed(ToolSideEffect.CommandExecution).Should().BeTrue();
        policy.IsAllowed(ToolSideEffect.ExternalSideEffect).Should().BeTrue();
    }

    [Fact]
    public void GrantAllowsOnlyListedSideEffects()
    {
        ToolPermissionPolicy policy = ToolPermissionPolicy.Grant([ToolSideEffect.LocalWrite]);
        policy.IsAllowed(ToolSideEffect.LocalWrite).Should().BeTrue();
        policy.IsAllowed(ToolSideEffect.ReadOnly).Should().BeFalse();
        policy.IsAllowed(ToolSideEffect.ExternalSideEffect).Should().BeFalse();
    }

    [Fact]
    public void PoliciesWithSameGrantsAreEqual()
    {
        ToolPermissionPolicy first = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly, ToolSideEffect.LocalWrite]);
        ToolPermissionPolicy second = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly, ToolSideEffect.LocalWrite]);
        second.Should().Be(first);
    }

    [Fact]
    public void PoliciesWithDifferentGrantsAreNotEqual()
    {
        ToolPermissionPolicy first = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly]);
        ToolPermissionPolicy second = ToolPermissionPolicy.Grant([ToolSideEffect.LocalWrite]);
        second.Should().NotBe(first);
    }

    [Fact]
    public void GrantIsOrderIndependent()
    {
        ToolPermissionPolicy first = ToolPermissionPolicy.Grant([ToolSideEffect.LocalWrite, ToolSideEffect.ReadOnly]);
        ToolPermissionPolicy second = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly, ToolSideEffect.LocalWrite]);
        second.Should().Be(first);
    }

    [Fact]
    public void GrantDeduplicates()
    {
        ToolPermissionPolicy duplicated = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly, ToolSideEffect.ReadOnly]);
        ToolPermissionPolicy single = ToolPermissionPolicy.Grant([ToolSideEffect.ReadOnly]);
        single.Should().Be(duplicated);
    }
}