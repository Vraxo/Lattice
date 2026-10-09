namespace Lattice.Core.Tests;
public sealed class EvidenceAnswerTests
{
    [Fact]
    public void AnsweredRequiresEvidence()
    {
        Assert.Throws<ArgumentException>(
            () => EvidenceAnswer.Answered("q", "t", Array.Empty<EvidenceReference>()));
    }
    [Fact]
    public void AnsweredCarriesEvidence()
    {
        var reference = new EvidenceReference("a.txt", 1, 1, "x");
        var answer = EvidenceAnswer.Answered("q", "t", new[] { reference });
        Assert.Equal(EvidenceAnswerStatus.Answered, answer.Status);
        Assert.Equal(reference, Assert.Single(answer.Evidence));
    }
    [Fact]
    public void InsufficientHasNoEvidence()
    {
        var answer = EvidenceAnswer.Insufficient("q", "t");
        Assert.Equal(EvidenceAnswerStatus.InsufficientEvidence, answer.Status);
        Assert.Empty(answer.Evidence);
    }
    [Fact]
    public void UnsupportedHasNoEvidence()
    {
        var answer = EvidenceAnswer.Unsupported("q", "t");
        Assert.Equal(EvidenceAnswerStatus.UnsupportedQuestion, answer.Status);
        Assert.Empty(answer.Evidence);
    }
    [Fact]
    public void AnswersWithSameContentAreEqual()
    {
        var reference = new EvidenceReference("a.txt", 1, 1, "x");
        Assert.Equal(
            EvidenceAnswer.Answered("q", "t", new[] { reference }),
            EvidenceAnswer.Answered("q", "t", new[] { reference }));
    }
    [Fact]
    public void EvidenceReferenceRejectsInvertedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EvidenceReference("a.txt", 5, 2, "x"));
    }
}