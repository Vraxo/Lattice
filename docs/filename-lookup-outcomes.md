# Filename lookup outcome pilot

Filename discovery is the first targeted use of a native C# 15 union in Lattice.
`WorkspaceFileLocator.ResolveByFileName` returns `WorkspaceFileLookupOutcome`:

| Case | Payload | Meaning |
|---|---|---|
| `WorkspaceFileFound` | Relative path | A completed scan found exactly one match. |
| `WorkspaceFileMissing` | Requested filename | A completed scan found no matches. |
| `WorkspaceFileAmbiguous` | Raw lookup metadata | Multiple matches; explicit selection is required. |
| `WorkspaceFileLookupIncomplete` | Raw lookup metadata | A bound was reached; absence or uniqueness is not proven. |
| `WorkspaceFileLookupFailed` | Error | The request or workspace could not be accessed safely. |

Desktop routing handles all five cases in a switch expression with no fallback arm,
so the compiler can identify missing cases when the union evolves. Incomplete scans
show the observed match count as a lower bound, preserve candidates and omitted
counts, and never automatically open a candidate or claim the file is missing.
Exact root-relative lookup and case-insensitive root-file precedence still happen
before this workspace-wide scan.

This is not a replacement for the general `Result<T>` API. The existing
`FindByFileName` method remains available for callers needing raw bounded scan
metadata. The outcome adapter uses that same traversal, cancellation, exclusions,
and link/reparse-point checks; it adds no filesystem access.

## Tooling boundary

Building this union requires the .NET 11 / C# 15 compiler used in CI. The embedded
`Microsoft.CodeAnalysis.CSharp` 4.14 parser is independent of that compiler and is
not upgraded by this pilot. It does not understand C# 15 union declarations;
syntax inspection of such source may report syntax errors, and workspace
structure discovery does not yet inventory unions. Updating the embedded parser
and declaration inventory is separate work, not a capability claimed here.
