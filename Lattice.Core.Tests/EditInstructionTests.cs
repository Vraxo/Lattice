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
        Assert.Equal(EditInstructionStatus.Executable, instruction.Status);
        Assert.True(instruction.IsExecutable);
        Assert.True(instruction.RequireExecutable().IsSuccess);
    }

    [Fact]
    public void ShortenIsRepresentedButNotExecutable()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.Shorten,
            EditTarget.Section("Alpha"),
            PreservationConstraint.PreserveMeaning);
        Assert.Equal(EditInstructionStatus.Unsupported, instruction.Status);
        Assert.False(instruction.IsExecutable);
    }

    [Fact]
    public void ChangeToneIsRepresentedButNotExecutable()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.ChangeTone,
            EditTarget.Section("Alpha"),
            PreservationConstraint.PreserveMeaning);
        Assert.False(instruction.IsExecutable);
    }

    [Fact]
    public void UnsupportedOperationIsRefusedWithATypedError()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.Shorten,
            EditTarget.Section("Alpha"));
        Result result = instruction.RequireExecutable();
        Assert.False(result.IsSuccess);
        Assert.Equal(EditErrorCodes.OperationUnsupported, result.Error!.Code);
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
        Assert.True(instruction.Constraints.HasFlag(PreservationConstraint.PreserveMeaning));
        Assert.True(instruction.Constraints.HasFlag(PreservationConstraint.RestrictToTarget));
        Assert.True(instruction.Constraints.HasFlag(PreservationConstraint.PreserveUntouchedContent));
    }

    [Fact]
    public void NoConstraintsIsNone()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.ReplaceSection,
            EditTarget.Section("Alpha"));
        Assert.Equal(PreservationConstraint.None, instruction.Constraints);
    }

    [Fact]
    public void ParametersArePreservedAndCaseSensitive()
    {
        EditInstruction instruction = EditInstruction.Create(
            EditOperationKind.ReplaceSection,
            EditTarget.Section("Alpha"),
            parameters: new Dictionary<string, string>(StringComparer.Ordinal) { ["level"] = "2" });
        Assert.Equal("2", instruction.Parameters["level"]);
        Assert.False(instruction.Parameters.ContainsKey("Level"));
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
        Assert.Equal("2", instruction.Parameters["level"]);
    }

    [Fact]
    public void TargetsDistinguishKindAndReference()
    {
        Assert.Equal(EditTargetKind.Section, EditTarget.Section("A").Kind);
        Assert.Equal(EditTargetKind.Path, EditTarget.Path("a.md").Kind);
        Assert.Equal(EditTargetKind.Document, EditTarget.Document("a.md").Kind);
        Assert.NotEqual(EditTarget.Section("A"), EditTarget.Section("B"));
    }

    [Fact]
    public void EmptyTargetReferenceIsRejected()
    {
        Assert.Throws<ArgumentException>(() => EditTarget.Section("  "));
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
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
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
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void InstructionsWithSameContentAreEqual()
    {
        EditInstruction a = EditInstruction.Create(EditOperationKind.ReplaceSection, EditTarget.Section("A"));
        EditInstruction b = EditInstruction.Create(EditOperationKind.ReplaceSection, EditTarget.Section("A"));
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}