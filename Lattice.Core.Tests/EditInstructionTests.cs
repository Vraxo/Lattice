namespace Lattice.Core.Tests;

/// <summary>
/// Verifies that edit instructions and preservation constraints can be represented and validated,
/// and that an unsupported semantic transformation is represented honestly and refused rather than
/// reported as applied.
/// </summary>
public sealed class EditInstructionTests
{
    [Fact]
    public void SectionReplacementIsExecutable()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.ReplaceSection,
            EditTarget.Section("Alpha"),
            PreservationConstraint.RestrictToTarget | PreservationConstraint.PreserveUntouchedContent);
        instruction.Status.Should().Be(EditInstructionStatus.Executable);
        instruction.IsExecutable.Should().BeTrue();
        instruction.RequireExecutable().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ShortenIsRepresentedButNotExecutable()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.Shorten,
            EditTarget.Section("Alpha"),
            PreservationConstraint.PreserveMeaning);
        instruction.Status.Should().Be(EditInstructionStatus.Unsupported);
        instruction.IsExecutable.Should().BeFalse();
    }

    [Fact]
    public void ChangeToneIsRepresentedButNotExecutable()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.ChangeTone,
            EditTarget.Section("Alpha"),
            PreservationConstraint.PreserveMeaning);
        instruction.IsExecutable.Should().BeFalse();
    }

    [Fact]
    public void UnsupportedOperationIsRefusedWithATypedError()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.Shorten,
            EditTarget.Section("Alpha"));
        Result result = instruction.RequireExecutable();
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(EditErrorCodes.OperationUnsupported);
    }

    [Fact]
    public void ConstraintsAreCombinableAndInspectable()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.ReplaceSection,
            EditTarget.Section("Alpha"),
            PreservationConstraint.PreserveMeaning
                | PreservationConstraint.RestrictToTarget
                | PreservationConstraint.PreserveUntouchedContent);
        instruction.Constraints.HasFlag(PreservationConstraint.PreserveMeaning).Should().BeTrue();
        instruction.Constraints.HasFlag(PreservationConstraint.RestrictToTarget).Should().BeTrue();
        instruction.Constraints.HasFlag(PreservationConstraint.PreserveUntouchedContent).Should().BeTrue();
    }

    [Fact]
    public void NoConstraintsIsNone()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.ReplaceSection,
            EditTarget.Section("Alpha"));
        instruction.Constraints.Should().Be(PreservationConstraint.None);
    }

    [Fact]
    public void ParametersArePreservedAndCaseSensitive()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.ReplaceSection,
            EditTarget.Section("Alpha"),
            parameters: new Dictionary<string, string>(StringComparer.Ordinal) { ["level"] = "2" });
        instruction.Parameters["level"].Should().Be("2");
        instruction.Parameters.ContainsKey("Level").Should().BeFalse();
    }

    [Fact]
    public void ParametersAreCopiedNotAliased()
    {
        Dictionary<string, string> source = new(StringComparer.Ordinal) { ["level"] = "2" };
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.ReplaceSection,
            EditTarget.Section("Alpha"),
            parameters: source);
        source["level"] = "3";
        instruction.Parameters["level"].Should().Be("2");
    }

    [Fact]
    public void TargetsDistinguishKindAndReference()
    {
        EditTarget.Section("A").Kind.Should().Be(EditTargetKind.Section);
        EditTarget.Path("a.md").Kind.Should().Be(EditTargetKind.Path);
        EditTarget.Document("a.md").Kind.Should().Be(EditTargetKind.Document);
        EditTarget.Section("B").Should().NotBe(EditTarget.Section("A"));
    }

    [Fact]
    public void EmptyTargetReferenceIsRejected()
    {
        FluentActions.Invoking(() => EditTarget.Section("  ")).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void InstructionsWithEqualButDistinctParameterMapsAreEqual()
    {
        // ImmutableDictionary compares by reference, so this pins content-based equality rather
        // than the record's synthesized reference comparison.
        EditInstruction a = EditInstruction.Create(
            EditOperationKind.ReplaceSection,
            EditTarget.Section("A"),
            PreservationConstraint.RestrictToTarget,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["level"] = "2", ["note"] = "x" });
        EditInstruction b = EditInstruction.Create(
            EditOperationKind.ReplaceSection,
            EditTarget.Section("A"),
            PreservationConstraint.RestrictToTarget,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["note"] = "x", ["level"] = "2" });
        b.Should().Be(a);
        b.GetHashCode().Should().Be(a.GetHashCode());
    }

    [Fact]
    public void InstructionsWithDifferentParametersAreNotEqual()
    {
        EditInstruction a = EditInstruction.Create(
            EditOperationKind.ReplaceSection,
            EditTarget.Section("A"),
            parameters: new Dictionary<string, string>(StringComparer.Ordinal) { ["level"] = "2" });
        EditInstruction b = EditInstruction.Create(
            EditOperationKind.ReplaceSection,
            EditTarget.Section("A"),
            parameters: new Dictionary<string, string>(StringComparer.Ordinal) { ["level"] = "3" });
        b.Should().NotBe(a);
    }

    [Fact]
    public void InstructionsWithSameContentAreEqual()
    {
        EditInstruction a = EditInstruction.Create(EditOperationKind.ReplaceSection, EditTarget.Section("A"));
        EditInstruction b = EditInstruction.Create(EditOperationKind.ReplaceSection, EditTarget.Section("A"));
        b.Should().Be(a);
        b.GetHashCode().Should().Be(a.GetHashCode());
    }
}