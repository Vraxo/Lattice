namespace Lattice.Core;
public sealed record ClarificationRequest
{
    public ClarificationRequest(ClarificationKind kind, string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Clarification question must not be empty.", nameof(question));
        }
        Kind = kind;
        Question = question;
    }
    public ClarificationKind Kind { get; }
    public string Question { get; }
}