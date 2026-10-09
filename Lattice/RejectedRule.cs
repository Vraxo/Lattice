namespace Lattice.Core;
public sealed record RejectedRule
{
    public RejectedRule(string ruleId, int priority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
        RuleId = ruleId;
        Priority = priority;
    }
    public string RuleId { get; }
    public int Priority { get; }
}