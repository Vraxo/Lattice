namespace Lattice.Core.Tests;

public sealed class EvidenceAnswerTests
{
    [Fact]
    public void AnsweredRequiresEvidence()
    {
        Assert.Throws<ArgumentException>(
            () => EvidenceAnswer.Answered("q", "t", []));
    }

    [Fact]
    public void AnsweredCarriesEvidence()
    {
        EvidenceReference reference = new("a.txt", 1, 1, "x");
        EvidenceAnswer answer = EvidenceAnswer.Answered("q", "t", [reference]);
        Assert.Equal(EvidenceAnswerStatus.Answered, answer.Status);
        Assert.Equal(reference, Assert.Single(answer.Evidence));
    }

    [Fact]
    public void InsufficientHasNoEvidence()
    {
        EvidenceAnswer answer = EvidenceAnswer.Insufficient("q", "t");
        Assert.Equal(EvidenceAnswerStatus.InsufficientEvidence, answer.Status);
        Assert.Empty(answer.Evidence);
    }

    [Fact]
    public void UnsupportedHasNoEvidence()
    {
        EvidenceAnswer answer = EvidenceAnswer.Unsupported("q", "t");
        Assert.Equal(EvidenceAnswerStatus.UnsupportedQuestion, answer.Status);
        Assert.Empty(answer.Evidence);
    }

    [Fact]
    public void AnswersWithSameContentAreEqual()
    {
        EvidenceReference reference = new("a.txt", 1, 1, "x");
        Assert.Equal(
            EvidenceAnswer.Answered("q", "t", [reference]),
            EvidenceAnswer.Answered("q", "t", [reference]));
    }

    [Fact]
    public void EvidenceReferenceRejectsInvertedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EvidenceReference("a.txt", 5, 2, "x"));
    }
}