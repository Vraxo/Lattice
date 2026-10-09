using System.Collections.Immutable;
namespace Lattice.Core;
/// <summary>
/// The structured result of interpreting one incoming statement. State changes (a recorded fact
/// or goal) are already applied to <see cref="Session"/>. When <see cref="Candidates"/> is
/// non-empty the turn is not yet decided and the candidates must go through selection and
/// execution; otherwise the turn is already resolved and <see cref="ResolvedOutcome"/> applies.
/// </summary>
public sealed record InterpretedStatement
{
    private InterpretedStatement(
        Session session,
        ImmutableArray<ActionProposal> candidates,
        TurnOutcome? resolvedOutcome,
        string? resolvedResponse)
    {
        Session = session;
        Candidates = candidates;
        ResolvedOutcome = resolvedOutcome;
        ResolvedResponse = resolvedResponse;
    }
    public Session Session { get; }
    public ImmutableArray<ActionProposal> Candidates { get; }
    public TurnOutcome? ResolvedOutcome { get; }
    public string? ResolvedResponse { get; }
    public bool IsResolved => ResolvedOutcome is not null;
    public static InterpretedStatement StateChange(Session session, TurnOutcome outcome, string response)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(response);
        return new InterpretedStatement(session, [], outcome, response);
    }
    public static InterpretedStatement Actionable(Session session, ActionProposal candidate)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(candidate);
        return new InterpretedStatement(session, [candidate], null, null);
    }
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct candidate sets as unequal.
    public bool Equals(InterpretedStatement? other)
    {
        if (other is null)
        {
            return false;
        }
        return Session == other.Session
            && ResolvedOutcome == other.ResolvedOutcome
            && ResolvedResponse == other.ResolvedResponse
            && Candidates.SequenceEqual(other.Candidates);
    }
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Session);
        hash.Add(ResolvedOutcome);
        hash.Add(ResolvedResponse);
        foreach (var candidate in Candidates)
        {
            hash.Add(candidate);
        }
        return hash.ToHashCode();
    }
}