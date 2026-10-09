namespace Lattice.Core.Tests;
public sealed class ActionProposalExhaustivenessTests
{
    /// <summary>
    /// Every concrete proposal type must be listed here. Adding a variant without updating
    /// this set fails the test, forcing the validator's switch to be reviewed in step.
    /// </summary>
    private static readonly string[] Expected =
    [
        "AskUserProposal",
        "ContinueProposal",
        "FinishProposal",
        "InvokeToolProposal",
        "RespondProposal",
    ];
    [Fact]
    public void EveryConcreteProposalTypeIsAccountedFor()
    {
        var actual = typeof(ActionProposal).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ActionProposal).IsAssignableFrom(type))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.True(
            Expected.SequenceEqual(actual, StringComparer.Ordinal),
            $"Expected: {string.Join(", ", Expected)}{Environment.NewLine}Actual: {string.Join(", ", actual)}");
    }
}