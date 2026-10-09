using System.Collections.Immutable;
namespace Lattice.Core;
public sealed record Session
{
    public Session(
        SessionId id,
        IEnumerable<Message>? messages = null,
        IEnumerable<Fact>? facts = null,
        IEnumerable<Goal>? goals = null,
        IEnumerable<Observation>? observations = null)
    {
        Id = id;
        Messages = messages?.ToImmutableArray() ?? ImmutableArray<Message>.Empty;
        Facts = facts?.ToImmutableArray() ?? ImmutableArray<Fact>.Empty;
        Goals = goals?.ToImmutableArray() ?? ImmutableArray<Goal>.Empty;
        Observations = observations?.ToImmutableArray() ?? ImmutableArray<Observation>.Empty;
    }
    public SessionId Id { get; }
    public ImmutableArray<Message> Messages { get; init; }
    public ImmutableArray<Fact> Facts { get; init; }
    public ImmutableArray<Goal> Goals { get; init; }
    public ImmutableArray<Observation> Observations { get; init; }
    public static Session Empty(SessionId id) => new(id);
    public Session AddMessage(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return this with { Messages = Messages.Add(message) };
    }
    public Session AddUserAssertion(string content, string source)
    {
        var fact = new Fact(FactId.New(), content, source, FactStatus.UserAsserted);
        return this with { Facts = Facts.Add(fact) };
    }
    public Session AddToolObservation(string content, string source)
    {
        var observation = new Observation(content, source);
        return this with { Observations = Observations.Add(observation) };
    }
    public Session AddGoal(Goal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);
        return this with { Goals = Goals.Add(goal) };
    }
    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct state as unequal.
    public bool Equals(Session? other)
    {
        if (other is null)
        {
            return false;
        }
        return Id == other.Id
            && Messages.SequenceEqual(other.Messages)
            && Facts.SequenceEqual(other.Facts)
            && Goals.SequenceEqual(other.Goals)
            && Observations.SequenceEqual(other.Observations);
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        foreach (var message in Messages)
        {
            hash.Add(message);
        }
        foreach (var fact in Facts)
        {
            hash.Add(fact);
        }
        foreach (var goal in Goals)
        {
            hash.Add(goal);
        }
        foreach (var observation in Observations)
        {
            hash.Add(observation);
        }
        return hash.ToHashCode();
    }
}