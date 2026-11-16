namespace Lattice.Core.Tests;

public sealed class DerivationTests
{
    [Fact]
    public void CarriesRulePremisesAndConclusion()
    {
        KnowledgeFact conclusion = new("derived.rule.a", "concept.a", "q", "w", "rule:rule.a");
        Derivation derivation = new("rule.a", ["fact.a", "fact.b"], conclusion);
        derivation.RuleId.Should().Be("rule.a");
        derivation.PremiseFactIds.Should().Equal(new[] { "fact.a", "fact.b" });
        derivation.Conclusion.Should().Be(conclusion);
    }

    [Fact]
    public void RejectsEmptyRuleId()
    {
        KnowledgeFact conclusion = new("derived.rule.a", "concept.a", "q", "w");
        FluentActions.Invoking(() => new Derivation(string.Empty, ["fact.a"], conclusion)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void DerivationsWithSameContentAreEqual()
    {
        KnowledgeFact conclusion = new("derived.rule.a", "concept.a", "q", "w", "rule:rule.a");
        Derivation first = new("rule.a", ["fact.a"], conclusion);
        Derivation second = new("rule.a", ["fact.a"], conclusion);
        second.Should().Be(first);
    }
}