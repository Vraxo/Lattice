namespace Lattice.Core.Tests;

public sealed class DerivationTests
{
    [Fact]
    public void CarriesRulePremisesAndConclusion()
    {
        KnowledgeFact conclusion = new("derived.rule.a", "concept.a", "q", "w", "rule:rule.a");
        Derivation derivation = new("rule.a", ["fact.a", "fact.b"], conclusion);
        Assert.Equal("rule.a", derivation.RuleId);
        Assert.Equal(new[] { "fact.a", "fact.b" }, derivation.PremiseFactIds);
        Assert.Equal(conclusion, derivation.Conclusion);
    }

    [Fact]
    public void RejectsEmptyRuleId()
    {
        KnowledgeFact conclusion = new("derived.rule.a", "concept.a", "q", "w");
        Assert.Throws<ArgumentException>(
            () => new Derivation(string.Empty, ["fact.a"], conclusion));
    }

    [Fact]
    public void DerivationsWithSameContentAreEqual()
    {
        KnowledgeFact conclusion = new("derived.rule.a", "concept.a", "q", "w", "rule:rule.a");
        Derivation first = new("rule.a", ["fact.a"], conclusion);
        Derivation second = new("rule.a", ["fact.a"], conclusion);
        Assert.Equal(first, second);
    }
}