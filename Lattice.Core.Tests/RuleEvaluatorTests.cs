using System.Collections.Immutable;

namespace Lattice.Core.Tests;

public sealed class RuleEvaluatorTests
{
    [Fact]
    public void EvaluatesTheExamplePackage()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "knowledge", "example.physics", "package.json");
        Assert.True(File.Exists(path), $"Example package not found at {path}.");
        Result<KnowledgePackage> parsed = KnowledgePackageParser.Parse(File.ReadAllText(path));
        Assert.True(parsed.IsSuccess, parsed.Error?.Message);
        ImmutableArray<Derivation> derivations = RuleEvaluator.Evaluate(parsed.Value.Rules, parsed.Value.Facts);
        Derivation derivation = Assert.Single(derivations);
        Assert.Equal("rule.water.boils", derivation.RuleId);
        Assert.Equal("concept.water", derivation.Conclusion.Subject);
        Assert.Equal("state", derivation.Conclusion.Predicate);
        Assert.Equal("boiling", derivation.Conclusion.Value);
        Assert.Equal(2, derivation.PremiseFactIds.Length);
    }

    [Fact]
    public void DerivesConclusionWhenAllPremisesPresent()
    {
        KnowledgeRule[] rules = [Rule("rule.a", "p", "v", "q", "w")];
        KnowledgeFact[] facts = [Fact("fact.a", "p", "v")];
        ImmutableArray<Derivation> derivations = RuleEvaluator.Evaluate(rules, facts);
        Derivation derivation = Assert.Single(derivations);
        Assert.Equal("q", derivation.Conclusion.Predicate);
        Assert.Equal("w", derivation.Conclusion.Value);
        Assert.Equal(new[] { "fact.a" }, derivation.PremiseFactIds);
    }

    [Fact]
    public void DoesNotDeriveWhenPremiseMissing()
    {
        KnowledgeRule[] rules = [Rule("rule.a", "p", "v", "q", "w")];
        KnowledgeFact[] facts = [Fact("fact.a", "p", "different")];
        Assert.Empty(RuleEvaluator.Evaluate(rules, facts));
    }

    [Fact]
    public void DoesNotRederiveAnExistingTriple()
    {
        KnowledgeRule[] rules = [Rule("rule.a", "p", "v", "q", "w")];
        KnowledgeFact[] facts =
        [
            Fact("fact.a", "p", "v"),
            Fact("fact.existing", "q", "w"),
        ];
        Assert.Empty(RuleEvaluator.Evaluate(rules, facts));
    }

    [Fact]
    public void CascadesAcrossRulesDeterministically()
    {
        KnowledgeRule[] rules =
        [
            Rule("rule.a", "p", "v", "q", "w"),
            Rule("rule.b", "q", "w", "r", "x"),
        ];
        KnowledgeFact[] facts = [Fact("fact.a", "p", "v")];
        ImmutableArray<Derivation> derivations = RuleEvaluator.Evaluate(rules, facts);
        Assert.Equal(2, derivations.Length);
        Assert.Equal("rule.a", derivations[0].RuleId);
        Assert.Equal("rule.b", derivations[1].RuleId);
        Assert.Equal("r", derivations[1].Conclusion.Predicate);
    }

    [Fact]
    public void TerminatesWhenRuleWouldRecreateItsOwnPremise()
    {
        // rule.a derives q=w; rule.b consumes q=w and derives p=v, which already exists.
        KnowledgeRule[] rules =
        [
            Rule("rule.a", "p", "v", "q", "w"),
            Rule("rule.b", "q", "w", "p", "v"),
        ];
        KnowledgeFact[] facts = [Fact("fact.a", "p", "v")];
        ImmutableArray<Derivation> derivations = RuleEvaluator.Evaluate(rules, facts);
        Assert.Single(derivations);
        Assert.Equal("rule.a", derivations[0].RuleId);
    }

    [Fact]
    public void EvaluationIsDeterministic()
    {
        KnowledgeRule[] rules =
        [
            Rule("rule.a", "p", "v", "q", "w"),
            Rule("rule.b", "q", "w", "r", "x"),
        ];
        KnowledgeFact[] facts = [Fact("fact.a", "p", "v")];
        ImmutableArray<Derivation> first = RuleEvaluator.Evaluate(rules, facts);
        ImmutableArray<Derivation> second = RuleEvaluator.Evaluate(rules, facts);
        // Compare element-wise: Assert.Equal on two ImmutableArray<T> values binds to the
        // generic overload and uses ImmutableArray's reference-based struct equality.
        Assert.True(first.SequenceEqual(second));
    }

    private static KnowledgeRule Rule(
        string id, string premisePredicate, string premiseValue, string conclusionPredicate, string conclusionValue)
    {
        return new(
            id,
            $"derives {conclusionPredicate}={conclusionValue}",
            [new KnowledgePattern("concept.a", premisePredicate, premiseValue)],
            new KnowledgePattern("concept.a", conclusionPredicate, conclusionValue));
    }

    private static KnowledgeFact Fact(string id, string predicate, string value)
    {
        return new(id, "concept.a", predicate, value);
    }
}