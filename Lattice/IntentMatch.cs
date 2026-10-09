namespace Lattice.Core;
public sealed record IntentMatch
{
    public IntentMatch(string patternId, RequestIntentKind intent, IntentSlot? slot)
    {
        if (string.IsNullOrWhiteSpace(patternId))
        {
            throw new ArgumentException("Pattern id must not be empty.", nameof(patternId));
        }
        if (intent == RequestIntentKind.Unknown)
        {
            throw new ArgumentException("A match must have a known intent.", nameof(intent));
        }
        PatternId = patternId;
        Intent = intent;
        Slot = slot;
    }
    public string PatternId { get; }
    public RequestIntentKind Intent { get; }
    public IntentSlot? Slot { get; }
}