using System.Collections.Immutable;

namespace Lattice.Core.Tests;

public sealed class CapabilityDiscoveryTests
{
    [Fact]
    public void MatchesToolIdExactly()
    {
        ToolCatalog catalog = Catalog(Descriptor("calculator", "Adds two integers."));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("calculator"));
        CapabilityCandidate candidate = Assert.Single(candidates);
        Assert.Equal("calculator", candidate.Proposal.ToolId.Value);
        Assert.Contains("id", candidate.MatchedOn);
    }

    [Fact]
    public void MatchingIsCaseInsensitive()
    {
        ToolCatalog catalog = Catalog(Descriptor("calculator", "Adds two integers."));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("CALCULATOR"));
        Assert.Single(candidates);
    }

    [Fact]
    public void MatchesAlias()
    {
        ToolCatalog catalog = Catalog(Descriptor("calculator", "Adds two integers.", aliases: ["add"]));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("add"));
        CapabilityCandidate candidate = Assert.Single(candidates);
        Assert.Contains("alias", candidate.MatchedOn);
    }

    [Fact]
    public void MatchesTag()
    {
        ToolCatalog catalog = Catalog(Descriptor("files.search", "Searches file contents.", tags: ["find-symbol"]));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("find-symbol"));
        CapabilityCandidate candidate = Assert.Single(candidates);
        Assert.Contains("tag", candidate.MatchedOn);
    }

    [Fact]
    public void MatchesDescriptionWord()
    {
        ToolCatalog catalog = Catalog(Descriptor("files.read", "Reads a UTF-8 text file inside the workspace."));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("reads"));
        CapabilityCandidate candidate = Assert.Single(candidates);
        Assert.Contains("description", candidate.MatchedOn);
    }

    [Fact]
    public void UniqueMatchIsReturned()
    {
        ToolCatalog catalog = Catalog(
            Descriptor("calculator", "Adds two integers."),
            Descriptor("clock", "Reports the current time."));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("calculator"));
        Assert.Single(candidates);
    }

    [Fact]
    public void AmbiguousMatchReturnsAllTopScoringCandidates()
    {
        ToolCatalog catalog = Catalog(
            Descriptor("a", "Does a thing.", tags: ["shared"]),
            Descriptor("b", "Does another thing.", tags: ["shared"]));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("shared"));
        Assert.Equal(2, candidates.Length);
        Assert.All(candidates, c => Assert.Equal(2, c.Score));
    }

    [Fact]
    public void NoMatchReturnsEmpty()
    {
        ToolCatalog catalog = Catalog(Descriptor("calculator", "Adds two integers."));
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("unrelated"));
        Assert.Empty(candidates);
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
        Assert.All(candidates, c => Assert.Equal(candidates[0].Score, c.Score));
    }

    [Fact]
    public void CandidateCarriesRequestArguments()
    {
        ToolCatalog catalog = Catalog(Descriptor("calculator", "Adds two integers."));
        ArgumentBag arguments = ArgumentBag.From([new ArgumentEntry("left", ArgumentValue.FromInteger(1))]);
        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("calculator", arguments));
        CapabilityCandidate candidate = Assert.Single(candidates);
        Assert.Equal(arguments, candidate.Proposal.Arguments);
    }

    [Fact]
    public void EmptyOperationIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new CapabilityRequest("  "));
    }

    [Fact]
    public void RequestWithoutArgumentsDefaultsToEmpty()
    {
        CapabilityRequest request = new("calculator");
        Assert.Empty(request.Arguments.Entries);
    }

    [Fact]
    public void DiscoveryIsDeterministic()
    {
        ToolCatalog catalog = Catalog(
            Descriptor("a", "Does a thing.", tags: ["shared"]),
            Descriptor("b", "Does another thing.", tags: ["shared"]));
        ImmutableArray<CapabilityCandidate> first = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("shared"));
        ImmutableArray<CapabilityCandidate> second = CapabilityDiscovery.Discover(catalog, new CapabilityRequest("shared"));

        // Compare element-wise: Assert.Equal on two ImmutableArray<T> values binds to the
        // generic overload and uses ImmutableArray's reference-based struct equality.
        Assert.True(first.SequenceEqual(second));
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