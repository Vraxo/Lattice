using System.Collections.Immutable;

namespace Lattice.Core;

public sealed record RequestInterpretation
{
    private RequestInterpretation(
        IntentMatchKind kind,
        RequestIntentKind intent,
        IntentMatch? match,
        ImmutableArray<IntentMatch> candidates,
        string text,
        SourceSpan span,
        string? diagnostic,
        string? missingSlotName)
    {
        Kind = kind;
        Intent = intent;
        Match = match;
        Candidates = candidates;
        Text = text;
        Span = span;
        Diagnostic = diagnostic;
        MissingSlotName = missingSlotName;
    }

    public IntentMatchKind Kind { get; }

    public RequestIntentKind Intent { get; }

    public IntentMatch? Match { get; }

    public ImmutableArray<IntentMatch> Candidates { get; }

    public string Text { get; }

    public SourceSpan Span { get; }

    public string? Diagnostic { get; }

    /// <summary>Gets the slot name that was left empty, when <see cref="Kind"/> is MissingValue.</summary>
    public string? MissingSlotName { get; }

    public static RequestInterpretation Matched(string text, IntentMatch match)
    {
        ArgumentNullException.ThrowIfNull(match);
        return new RequestInterpretation(
            IntentMatchKind.Matched,
            match.Intent,
            match,
            [],
            text,
            new SourceSpan(0, text.Length),
            null,
            null);
    }

    public static RequestInterpretation Unknown(string text, string diagnostic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnostic);
        return new RequestInterpretation(
            IntentMatchKind.Unknown,
            RequestIntentKind.Unknown,
            null,
            [],
            text,
            new SourceSpan(0, text.Length),
            diagnostic,
            null);
    }

    public static RequestInterpretation Ambiguous(string text, IEnumerable<IntentMatch> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ImmutableArray<IntentMatch> array = [.. candidates];
        if (array.Length < 2)
        {
            throw new ArgumentException("An ambiguous interpretation requires at least two candidates.", nameof(candidates));
        }

        return new RequestInterpretation(
            IntentMatchKind.Ambiguous,
            RequestIntentKind.Unknown,
            null,
            array,
            text,
            new SourceSpan(0, text.Length),
            "More than one pattern matched with equal specificity.",
            null);
    }

    public static RequestInterpretation MissingValue(string text, IntentMatch match, string missingSlotName)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentException.ThrowIfNullOrWhiteSpace(missingSlotName);
        return new RequestInterpretation(
            IntentMatchKind.MissingValue,
            match.Intent,
            match,
            [],
            text,
            new SourceSpan(0, text.Length),
            $"The value for '{missingSlotName}' is missing.",
            missingSlotName);
    }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct candidate sets as unequal.
    public bool Equals(RequestInterpretation? other)
    {
        if (other is null)
        {
            return false;
        }

        return Kind == other.Kind
            && Intent == other.Intent
            && Match == other.Match
            && Text == other.Text
            && Span == other.Span
            && Diagnostic == other.Diagnostic
            && MissingSlotName == other.MissingSlotName
            && Candidates.SequenceEqual(other.Candidates);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Kind);
        hash.Add(Intent);
        hash.Add(Match);
        hash.Add(Text);
        hash.Add(Span);
        hash.Add(Diagnostic);
        hash.Add(MissingSlotName);
        foreach (IntentMatch candidate in Candidates)
        {
            hash.Add(candidate);
        }

        return hash.ToHashCode();
    }
}