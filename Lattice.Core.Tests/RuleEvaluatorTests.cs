using System.Collections.Immutable;

namespace Lattice.Core.Tests;

public sealed class RuleEvaluatorTests
{
    [Fact]
    public void EvaluatesTheExamplePackage()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "knowledge", "example.physics", "package.json");
        File.Exists(path).Should().BeTrue("{0}", $"Example package not found at {path}." ?? string.Empty);
        Result<KnowledgePackage> parsed = KnowledgePackageParser.Parse(File.ReadAllText(path));
        parsed.IsSuccess.Should().BeTrue("{0}", parsed.Error?.Message ?? string.Empty);
        ImmutableArray<Derivation> derivations = RuleEvaluator.Evaluate(parsed.Value.Rules, parsed.Value.Facts);
        Derivation derivation = derivations.Should().ContainSingle().Which;
        derivation.RuleId.Should().Be("rule.water.boils");
        derivation.Conclusion.Subject.Should().Be("concept.water");
        derivation.Conclusion.Predicate.Should().Be("state");
        derivation.Conclusion.Value.Should().Be("boiling");
        derivation.PremiseFactIds.Length.Should().Be(2);
    }

    [Fact]
    public void DerivesConclusionWhenAllPremisesPresent()
    {
        KnowledgeRule[] rules = [Rule("rule.a", "p", "v", "q", "w")];
        KnowledgeFact[] facts = [Fact("fact.a", "p", "v")];
        ImmutableArray<Derivation> derivations = RuleEvaluator.Evaluate(rules, facts);
        Derivation derivation = derivations.Should().ContainSingle().Which;
        derivation.Conclusion.Predicate.Should().Be("q");
        derivation.Conclusion.Value.Should().Be("w");
        derivation.PremiseFactIds.Should().Equal(new[] { "fact.a" });
    }

    [Fact]
    public void DoesNotDeriveWhenPremiseMissing()
    {
        KnowledgeRule[] rules = [Rule("rule.a", "p", "v", "q", "w")];
        KnowledgeFact[] facts = [Fact("fact.a", "p", "different")];
        RuleEvaluator.Evaluate(rules, facts).Should().BeEmpty();
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
        RuleEvaluator.Evaluate(rules, facts).Should().BeEmpty();
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
        derivations.Length.Should().Be(2);
        derivations[0].RuleId.Should().Be("rule.a");
        derivations[1].RuleId.Should().Be("rule.b");
        derivations[1].Conclusion.Predicate.Should().Be("r");
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
        derivations.Should().ContainSingle();
        derivations[0].RuleId.Should().Be("rule.a");
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

        // Compare elements in order rather than ImmutableArray backing-storage identity.
        second.Should().Equal(first);
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