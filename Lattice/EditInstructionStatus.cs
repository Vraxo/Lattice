namespace Lattice.Core;

/// <summary>Whether the current editor can perform a represented edit instruction.</summary>
public enum EditInstructionStatus
{
    Executable,
    Unsupported,
}