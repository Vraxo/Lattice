# Lattice

Lattice is a **symbolic, tool-using coding assistant** built around explicit state, bounded actions, and evidence from the user's workspace. It does not use an LLM or a trained generative model.

## Desktop application

`Lattice.Desktop` provides a neutral charcoal/dark-gray conversational desktop interface (`PhotinoX.Blazor` 5.3.5) for connecting a local code workspace, opening UTF-8 files by relative path or workspace-wide basename discovery, and running grounded source-text searches with visible file-and-line citations.

The active assignment is [`.agents/todo.md`](.agents/todo.md). Completed Desktop 0.1 and Desktop 0.2 checklists are preserved under [`.agents/archive/`](.agents/archive/).

> **Verification status:** Restore, Release build, workspace symlink/reparse-point safety tests, basename file-discovery tests, charcoal stylesheet verification, and headless xUnit/bUnit UI tests pass in Windows CI (.NET 10). Final interactive Photino window verification on a desktop display is tracked in [docs/windows-smoke-test.md](docs/windows-smoke-test.md) and [.agents/todo.md](.agents/todo.md).

### Prerequisites

- **.NET 10 SDK** (`10.0.x`).
- **Windows 10/11** with the Microsoft Edge WebView2 Runtime (preinstalled on current Windows 10/11 releases) to launch the native Photino desktop window, or another desktop OS with Photino's native WebView prerequisites installed.

### Build, test, and launch

```powershell
dotnet restore Lattice.slnx
dotnet build Lattice.slnx --configuration Release --no-restore
dotnet test Lattice.slnx --configuration Release --no-build
dotnet run --project Lattice.Desktop/Lattice.Desktop.csproj --configuration Release
```

The legacy controlled-statement console harness remains available separately:

```powershell
dotnet run --project Lattice.Cli
```

### Conversational workspace interaction (Desktop 0.3)

Connect a local project folder in the header bar, then ask about files or identifiers in ordinary conversational English in the message composer (`Enter` sends; `Shift+Enter` inserts a newline):

- **Viewing workspace files (exact relative path or basename discovery)**
  - `"Show me Lattice/WorkspaceRoot.cs."` (exact relative path takes precedence)
  - `"Show me WorkspaceRoot.cs."` (unique nested basename opens `Lattice/WorkspaceRoot.cs` directly)
  - `"Show me Program.cs."` (when multiple files share the basename, Lattice lists their full relative paths — e.g. `Lattice.Cli/Program.cs`, `Lattice.Desktop/Program.cs` — and asks which one to open without guessing; reply with `Lattice.Desktop/Program.cs` or `"Show me Lattice.Desktop/Program.cs."` to open it)
  - `"Open the file README.md."`, `"Can you read Lattice/WorkspaceRoot.cs?"`, `"What's in README.md?"`
- **Finding source occurrences**
  - `"Where does WorkspaceRoot appear?"`
  - `"Where is WorkspaceRoot used in this project?"`
  - `"Can you find where WorkspaceRoot is used throughout this project?"`
  - `"Search this project for WorkspaceRoot."`
  - `"Find all occurrences of WorkspaceRoot."`
- **Narrow follow-up references**
  - `"Show me that file."` or `"Open it."` opens the file when the immediately preceding turn identifies **one unambiguous file**; if multiple files are in context (or no recent file is in context), Lattice asks which file you mean rather than guessing.

User messages and evidence-grounded assistant replies remain visible in chronological order during the session, styled with a cohesive neutral charcoal/dark-gray palette. Compatibility aliases (`find symbol <name>` and `inspect <relative-path>`) remain accepted, while unsupported or ambiguous messages receive plain-language explanations or targeted clarification questions without raw error codes or internal pattern IDs.

### Current limitations and safety boundaries

- **Non-LLM bounded phrase interpretation:** Requests are matched deterministically against an explicit, inspectable phrase catalog (`config/nlp/patterns.json` + `RequestInterpreter`) with at most one captured identifier or path slot. Lattice does not use an LLM, embeddings, network calls, or unrestricted English comprehension; unsupported or competing phrasings are surfaced honestly rather than guessed.
- **Deterministic basename discovery (separate from content search):** Bare filename requests (`"Show me Program.cs"`) check the exact root-relative file first, then scan non-linked directory entries by exact case-insensitive basename. If the request has no extension and no exact filename exists, Lattice falls back to exact case-insensitive filename-stem matching (for example, `"program"` matches `Program.cs`) (skipping `.git`, `.vs`, `artifacts`, `bin`, and `obj`, bounded to 100 collected paths / 500 counted matches and displaying up to 10 candidates inline with omitted counts). Candidate files are never read until a single path is unambiguous, and an explicit relative path is never replaced by a same-basename match elsewhere.
- **Read-only workspace access:** `Lattice.Desktop` registers only `FileSearchTool`, `ReadFileTool`, and `ListDirectoryTool` under `ToolPermissionPolicy.ReadOnlyOnly`. No file writes, patch application, or shell/command execution capabilities are registered.
- **Literal source-text search (not semantic resolution):** Source searches perform case-insensitive literal substring matching over UTF-8 workspace files (scanning up to 100 matches and displaying up to 20 citations; skipping `.git`, `.vs`, `artifacts`, `bin`, and `obj`). They do **not** perform Roslyn or AST declaration/reference resolution.
- **Narrow single-file follow-up context:** Follow-up references (`"Show me that file"`, `"Open it"`) and candidate selections track only the most recent turn's unambiguous single file or candidate file list, route through the same read-only `ReadFileTool` boundary, and clear automatically whenever the connected workspace changes.
- **File size and encoding bounds:** File reads and searches skip or reject files larger than 262,144 bytes and reject invalid UTF-8 binary data rather than substituting replacement characters.
- **Static symlink and reparse-point guard:** Workspace operations are confined to the explicitly connected root. Direct reads/directory resolution reject root-to-target paths containing symbolic links or reparse points, and directory/search/basename traversal skips linked files and linked directories. This is a conservative **static** check at resolution/enumeration time and is **not** race-resistant (TOCTOU-resistant) against concurrent filesystem link replacement.
- **Untrusted workspace text:** Inspected file text and search snippets are treated purely as passive data and are never reparsed or executed as instructions.

## Solution layout

| Project | Purpose |
|---|---|
| `Lattice/` | `Lattice.Core` — UI-independent domain types, agent loop, bounded phrase interpreter, `WorkspaceFileLocator`, workspace tools, safety boundary, and retrieval |
| `Lattice.Desktop/` | Desktop 0.3 executable host (`PhotinoX.Blazor` 5.3.5) and neutral charcoal conversational workspace UI |
| `Lattice.Desktop.Tests/` | Desktop session, phrase routing, basename discovery, follow-up context, theme stylesheet, and rendered bUnit chat tests |
| `Lattice.CSharp/` | Optional Roslyn syntax/semantic inspection and narrow syntax-edit library |
| `Lattice.Python/` | Optional Python AST syntax adapter |
| `Lattice.Cli/` | Limited controlled-statement console harness |
| `Lattice.Core.Tests/` | Core unit, safety-boundary, file-locator, and regression corpus tests |
| `knowledge/`, `config/`, `tests/` | Example knowledge package, intent patterns, and test corpora |

## Documentation

- [Active Desktop 0.3 build plan](.agents/todo.md) — ordered phases, verification notes, and remaining manual GUI checks.
- [Completed Desktop 0.1 & 0.2 archives](.agents/archive/) — earlier desktop milestone records.
- [Windows desktop smoke test](docs/windows-smoke-test.md) — step-by-step manual Photino launch walkthrough.
- [Desktop product direction](docs/DESKTOP_DIRECTION.md) — product scope and current milestone direction.
- [Documentation index](docs/README.md)
- [Historical research-first specifications and milestone records](docs/archive/research-first/) — retained for provenance, not active instructions.
