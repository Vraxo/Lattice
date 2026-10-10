namespace Lattice.Core;

public interface IActionRule
{
    string Id { get; }

    /// <summary>Gets higher values are considered first.</summary>
    int Priority { get; }

    /// <summary>
    /// Returns a proposal when this rule applies, or <see langword="null"/> when it does not.
    /// A rule must not throw to signal inapplicability.
    /// </summary>
    /// <returns></returns>
    ActionProposal? TryPropose(ActionSelectionContext context);
}