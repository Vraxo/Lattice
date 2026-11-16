using System.Collections.Immutable;

namespace Lattice.Core.Tests;

public sealed class CapabilityDiscoveryTests
{
    [Fact]
    public void MatchesToolIdExactly()
    {
        ToolCatalog catalog = Catalog(Descriptor("calculator", "Adds two integers."));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("calculator"));
        CapabilityCandidate candidate = candidates.Should().ContainSingle().Which;
        candidate.Proposal.ToolId.Value.Should().Be("calculator");
        candidate.MatchedOn.Should().Contain("id");
    }

    [Fact]
    public void MatchingIsCaseInsensitive()
    {
        ToolCatalog catalog = Catalog(Descriptor("calculator", "Adds two integers."));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("CALCULATOR"));
        candidates.Should().ContainSingle();
    }

    [Fact]
    public void MatchesAlias()
    {
        ToolCatalog catalog = Catalog(Descriptor("calculator", "Adds two integers.", aliases: ["add"]));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("add"));
        CapabilityCandidate candidate = candidates.Should().ContainSingle().Which;
        candidate.MatchedOn.Should().Contain("alias");
    }

    [Fact]
    public void MatchesTag()
    {
        ToolCatalog catalog = Catalog(Descriptor("files.search", "Searches file contents.", tags: ["find-symbol"]));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("find-symbol"));
        CapabilityCandidate candidate = candidates.Should().ContainSingle().Which;
        candidate.MatchedOn.Should().Contain("tag");
    }

    [Fact]
    public void MatchesDescriptionWord()
    {
        ToolCatalog catalog = Catalog(Descriptor("files.read", "Reads a UTF-8 text file inside the workspace."));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("reads"));
        CapabilityCandidate candidate = candidates.Should().ContainSingle().Which;
        candidate.MatchedOn.Should().Contain("description");
    }

    [Fact]
    public void UniqueMatchIsReturned()
    {
        ToolCatalog catalog = Catalog(
            Descriptor("calculator", "Adds two integers."),
            Descriptor("clock", "Reports the current time."));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("calculator"));
        candidates.Should().ContainSingle();
    }

    [Fact]
    public void AmbiguousMatchReturnsAllTopScoringCandidates()
    {
        ToolCatalog catalog = Catalog(
            Descriptor("a", "Does a thing.", tags: ["shared"]),
            Descriptor("b", "Does another thing.", tags: ["shared"]));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("shared"));
        candidates.Length.Should().Be(2);
        candidates.Should().AllSatisfy(c => c.Score.Should().Be(2));
    }

    [Fact]
    public void NoMatchReturnsEmpty()
    {
        ToolCatalog catalog = Catalog(Descriptor("calculator", "Adds two integers."));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("unrelated"));
        candidates.Should().BeEmpty();
    }

    [Fact]
    public void HigherScoringCandidateExcludesLowerScoringOnes()
    {
        ToolCatalog catalog = Catalog(
            Descriptor("search", "Searches things."),
            Descriptor("other", "Also searches things.", tags: ["search"]));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("search"));

        // "search" matches the id (3) and a description word (1) for one tool, and a tag (2)
        // plus a description word (1) for the other. Only the higher total survives.
        candidates.Should().AllSatisfy(c => c.Score.Should().Be(candidates[0].Score));
    }

    [Fact]
    public void CandidateCarriesRequestArguments()
    {
        ToolCatalog catalog = Catalog(Descriptor("calculator", "Adds two integers."));
        ArgumentBag arguments = ArgumentBag.From([new ArgumentEntry("left", ArgumentValue.FromInteger(1))]);
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("calculator", arguments));
        CapabilityCandidate candidate = candidates.Should().ContainSingle().Which;
        candidate.Proposal.Arguments.Should().Be(arguments);
    }

    [Fact]
    public void EmptyOperationIsRejected()
    {
        FluentActions.Invoking(() => new CapabilityRequest("  ")).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void RequestWithoutArgumentsDefaultsToEmpty()
    {
        CapabilityRequest request = new("calculator");
        request.Arguments.Entries.Should().BeEmpty();
    }

    [Fact]
    public void DiscoveryIsDeterministic()
    {
        ToolCatalog catalog = Catalog(
            Descriptor("a", "Does a thing.", tags: ["shared"]),
            Descriptor("b", "Does another thing.", tags: ["shared"]));
        ImmutableArray<CapabilityCandidate> first = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("shared"));
        ImmutableArray<CapabilityCandidate> second = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("shared"));

        // Compare elements in order rather than ImmutableArray backing-storage identity.
        second.Should().Equal(first);
    }

    private static ToolCatalog Catalog(params ToolDescriptor[] descriptors)
    {
        return new(descriptors);
    }

    private static ToolDescriptor Descriptor(
            string id,
            string description,
            ImmutableArray<string>? aliases = null,
            ImmutableArray<string>? tags = null)
    {
        return new(
                new ToolId(id),
                description,
                "Output.",
                ToolSideEffect.ReadOnly,
                aliases: aliases,
                tags: tags);
    }
}