namespace Lattice.Core;
public enum TurnOutcome
{
    ToolSucceeded,
    ToolFailed,
    ToolUnavailable,
    PermissionDenied,
    InvalidArguments,
    DuplicateAction,
    FactRecorded,
    GoalRecorded,
    ConstraintNotSupported,
    AskedUser,
    Responded,
    Blocked,
}