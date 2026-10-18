namespace Lattice.Core;

/// <summary>
/// Which edit operations the current editor can actually perform. The support table is kept small
/// and explicit so an unsupported operation is refused rather than silently attempted.
/// </summary>
public static class EditOperationSupport
{
    public static bool IsExecutable(EditOperationKind operation)
    {
        return operation == EditOperationKind.ReplaceSection;
    }

    public static EditInstructionStatus StatusOf(EditOperationKind operation)
    {
        return IsExecutable(operation)
            ? EditInstructionStatus.Executable
            : EditInstructionStatus.Unsupported;
    }
}