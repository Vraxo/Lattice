# Lattice

Lattice is a **symbolic, tool-using coding assistant** built around explicit state, bounded actions, and evidence from the user's workspace. It does not use an LLM or a trained generative model.

## Desktop application

Desktop 0.1 established the read-only workspace workflow. **Desktop 0.2**, merged in [PR #12](https://github.com/Vraxo/Lattice/pull/12), added the chat interface, natural-language requests, and evidence-grounded replies. The current application is conversational; its next increment adds workspace-wide basename discovery and a neutral charcoal theme.

The active assignment is [`.agents/todo.md`](.agents/todo.md). The Desktop 0.1 and 0.2 implementation plans are preserved under [`.agents/archive/`](.agents/archive/).

### Current desktop baseline (`Lattice.Desktop`)

Lattice Desktop 0.1 provides a read-only PhotinoX.Blazor desktop window for connecting a local code workspace, inspecting UTF-8 files, and running grounded source-text symbol searches with visible file-and-line citations.

> **Verification status:** Restore, Release build, workspace symlink/reparse-point safety tests, and headless xUnit/bUnit UI tests pass in Windows CI (.NET 10). Final interactive Photino window verification on a desktop display is tracked in [docs/windows-smoke-test.md](docs/windows-smoke-test.md) and [.agents/todo.md](.agents/todo.md).

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

### Conversational workspace interaction (Desktop 0.2)

Connect a local project folder in the header bar, then ask about files or identifiers in ordinary conversational English in the message composer (`Enter` sends; `Shift+Enter` inserts a newline):

- **Finding source occurrences**
  - `"Where does WorkspaceRoot appear?"`
  - `"Where is WorkspaceRoot used in this project?"`
  - `"Can you find where WorkspaceRoot is used throughout this project?"`
  - `"Search this project for WorkspaceRoot."`
  - `"Find all occurrences of WorkspaceRoot."`
- **Viewing workspace files**
  - `"Show me Lattice/WorkspaceRoot.cs."`
  - `"Open the file Lattice/WorkspaceRoot.cs."`
  - `"Can you read Lattice/WorkspaceRoot.cs?"`
  - `"What's in Lattice/WorkspaceRoot.cs?"`
- **Narrow follow-up references**
  - `"Show me that file."` or `"Open it."` opens the file when the immediately preceding turn identifies **one unambiguous file**; if the preceding search matched multiple files (or no recent file is in context), Lattice asks which file you mean rather than guessing.

User messages and evidence-grounded assistant replies remain visible in chronological order during the session, with relative `path:line` citations, matching snippets, and inline read-only file viewers. Compatibility aliases (`find symbol <name>` and `inspect <relative-path>`) remain accepted, while unsupported or ambiguous messages receive plain-language explanations or targeted clarification questions without raw error codes or internal pattern IDs.

### Current limitations and safety boundaries

- **Non-LLM bounded phrase interpretation:** Requests are matched deterministically against an explicit, inspectable phrase catalog (`config/nlp/patterns.json` + `RequestInterpreter`) with at most one captured identifier or path slot. Lattice does not use an LLM, embeddings, network calls, or unrestricted English comprehension; unsupported or competing phrasings are surfaced honestly rather than guessed.
- **Read-only workspace access:** `Lattice.Desktop` registers only `FileSearchTool`, `ReadFileTool`, and `ListDirectoryTool` under `ToolPermissionPolicy.ReadOnlyOnly`. No file writes, patch application, or shell/command execution capabilities are registered.
- **Literal source-text search (not semantic resolution):** Source searches perform case-insensitive literal substring matching over UTF-8 workspace files (scanning up to 100 matches and displaying up to 20 citations; skipping `.git`, `.vs`, `artifacts`, `bin`, and `obj`). They do **not** perform Roslyn or AST declaration/reference resolution.
- **Narrow single-file follow-up context:** Follow-up references (`"Show me that file"`, `"Open it"`) track only the most recent turn's unambiguous single file or candidate file list, route through the same read-only `ReadFileTool` boundary, and clear automatically whenever the connected workspace changes.
- **File size and encoding bounds:** File reads and searches skip or reject files larger than 262,144 bytes and reject invalid UTF-8 binary data rather than substituting replacement characters.
- **Static symlink and reparse-point guard:** Workspace operations are confined to the explicitly connected root. Direct reads/directory resolution reject root-to-target paths containing symbolic links or reparse points, and directory/search traversal skips linked files and linked directories. This is a conservative **static** check at resolution/enumeration time and is **not** race-resistant (TOCTOU-resistant) against concurrent filesystem link replacement.
- **Untrusted workspace text:** Inspected file text and search snippets are treated purely as passive data and are never reparsed or executed as instructions.

## Solution layout

| Project | Purpose |
|---|---|
| `Lattice/` | `Lattice.Core` — UI-independent domain types, agent loop, bounded phrase interpreter, workspace tools, safety boundary, and retrieval |
| `Lattice.Desktop/` | Desktop 0.2 executable host (`PhotinoX.Blazor` 5.3.5) and conversational read-only workspace UI |
| `Lattice.Desktop.Tests/` | Desktop session, phrase routing, follow-up context, and rendered bUnit chat component tests |
| `Lattice.CSharp/` | Optional Roslyn syntax/semantic inspection and narrow syntax-edit library |
| `Lattice.Python/` | Optional Python AST syntax adapter |
| `Lattice.Cli/` | Limited controlled-statement console harness |
| `Lattice.Core.Tests/` | Core unit, safety-boundary, and regression corpus tests |
| `knowledge/`, `config/`, `tests/` | Example knowledge package, intent patterns, and test corpora |

## Documentation

- [Active Desktop 0.2 build plan](.agents/todo.md) — ordered phases, verification notes, and remaining manual GUI checks.
- [Completed Desktop 0.1 archive](.agents/archive/desktop-0.1.md) — baseline desktop slice record.
- [Windows desktop smoke test](docs/windows-smoke-test.md) — step-by-step manual Photino launch walkthrough.
- [Desktop product direction](docs/DESKTOP_DIRECTION.md) — product scope and current milestone direction.
- [Documentation index](docs/README.md)
- [Historical research-first specifications and milestone records](docs/archive/research-first/) — retained for provenance, not active instructions.


### Next increment: file discovery and charcoal theme (Desktop 0.3)

- `Show me Program.cs` will search nested folders when no root-level file exists.
- A unique basename match will open directly; duplicate names will be listed by relative path and require explicit selection.
- The UI palette is being shifted from blue-black to neutral dark gray/charcoal.

See the [active Desktop 0.3 assignment](.agents/todo.md).
