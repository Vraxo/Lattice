using System.Collections.Immutable;

namespace Lattice.Core;
/// <summary>
/// An answer grounded in workspace evidence. An <see cref="EvidenceAnswerStatus.Answered"/>
/// answer must carry at least one <see cref="EvidenceReference"/>; that invariant is enforced
/// here rather than left to callers, so an unsupported claim cannot be constructed.
/// </summary>
public sealed record EvidenceAnswer
{
    private EvidenceAnswer(
        string question,
        EvidenceAnswerStatus status,
        string text,
        ImmutableArray<EvidenceReference> evidence)
    {
        Question = question;
        Status = status;
        Text = text;
        Evidence = evidence;
    }

    public string Question { get; }

    public EvidenceAnswerStatus Status { get; }

    public string Text { get; }

    public ImmutableArray<EvidenceReference> Evidence { get; }

    public static EvidenceAnswer Answered(
        string question,
        string text,
        IEnumerable<EvidenceReference> evidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(evidence);
        ImmutableArray<EvidenceReference> array = [.. evidence];
        if (array.IsEmpty)
        {
            throw new ArgumentException(
                "An answered question must cite at least one piece of evidence.",
                nameof(evidence));
        }

        return new EvidenceAnswer(question, EvidenceAnswerStatus.Answered, text, array);
    }

    public static EvidenceAnswer Insufficient(string question, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return new EvidenceAnswer(question, EvidenceAnswerStatus.InsufficientEvidence, text, []);
    }

    public static EvidenceAnswer Unsupported(string question, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return new EvidenceAnswer(question, EvidenceAnswerStatus.UnsupportedQuestion, text, []);
    }

    // ImmutableArray<T> compares by reference of its backing array, so the record's
    // synthesized equality would treat equal-but-distinct evidence sets as unequal.
    public bool Equals(EvidenceAnswer? other)
    {
        if (other is null)
        {
            return false;
        }

        return Question == other.Question
            && Status == other.Status
            && Text == other.Text
            && Evidence.SequenceEqual(other.Evidence);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Question);
        hash.Add(Status);
        hash.Add(Text);
        foreach (EvidenceReference reference in Evidence)
        {
            hash.Add(reference);
        }

        return hash.ToHashCode();
    }
}