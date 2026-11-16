namespace Lattice.Core.Tests;

public sealed class EvidenceAnswerTests
{
    [Fact]
    public void AnsweredRequiresEvidence()
    {
        FluentActions.Invoking(() => EvidenceAnswer.Answered("q", "t", [])).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void AnsweredCarriesEvidence()
    {
        EvidenceReference reference = new("a.txt", 1, 1, "x");
        EvidenceAnswer answer = EvidenceAnswer.Answered("q", "t", [reference]);
        answer.Status.Should().Be(EvidenceAnswerStatus.Answered);
        answer.Evidence.Should().ContainSingle().Which.Should().Be(reference);
    }

    [Fact]
    public void InsufficientHasNoEvidence()
    {
        EvidenceAnswer answer = EvidenceAnswer.Insufficient("q", "t");
        answer.Status.Should().Be(EvidenceAnswerStatus.InsufficientEvidence);
        answer.Evidence.Should().BeEmpty();
    }

    [Fact]
    public void UnsupportedHasNoEvidence()
    {
        EvidenceAnswer answer = EvidenceAnswer.Unsupported("q", "t");
        answer.Status.Should().Be(EvidenceAnswerStatus.UnsupportedQuestion);
        answer.Evidence.Should().BeEmpty();
    }

    [Fact]
    public void AnswersWithSameContentAreEqual()
    {
        EvidenceReference reference = new("a.txt", 1, 1, "x");
        EvidenceAnswer.Answered("q", "t", [reference]).Should().Be(EvidenceAnswer.Answered("q", "t", [reference]));
    }

    [Fact]
    public void EvidenceReferenceRejectsInvertedRange()
    {
        FluentActions.Invoking(() => new EvidenceReference("a.txt", 5, 2, "x")).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }
}