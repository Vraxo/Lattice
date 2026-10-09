namespace Lattice.Core;
/// <summary>
/// A proposed next action for the agent loop. Proposals are data that must be validated and
/// ranked before execution; they are never strings to be reparsed (ADR-002).
/// </summary>
public abstract record ActionProposal;