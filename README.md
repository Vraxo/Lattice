# Lattice

Lattice is a **symbolic, tool-using coding assistant** built around explicit state, bounded actions, and evidence from the user's workspace. It does not use an LLM or a trained generative model.

## Desktop application

Desktop 0.1 delivered a read-only PhotinoX.Blazor workspace interface. **Desktop 0.2 is the next active increment:** replacing its command-like request form with a conversational interface and ordinary bounded natural-language phrasing.

The active assignment is [`.agents/todo.md`](.agents/todo.md). The 0.1 implementation checklist is archived under [`.agents/archive/desktop-0.1.md`](.agents/archive/desktop-0.1.md).

### Desktop 0.1 baseline (`Lattice.Desktop`)

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

### Supported requests

The currently merged Desktop 0.1 baseline accepts these read-only request forms; Desktop 0.2 is replacing the command-oriented interaction with natural phrasing:

| Request form | Behavior | Example |
|---|---|---|
| `inspect <relative-path>` | Reads a UTF-8 text file inside the connected workspace root and renders its contents in a scrollable pane. | `inspect Lattice/WorkspaceRoot.cs` |
| `find symbol <name>` | Runs bounded, case-insensitive **literal source-text search** across workspace files and displays matching `path:line` citations and snippets. | `find symbol WorkspaceQuestionAnswerer` |

Prior requests, summaries, inspected file contents, and search citations stay visible in chronological order during the session. Distinct UI states are shown for unsupported input, ambiguous input (such as `show README.md`), no matches, invalid/unconnected workspaces, missing files, access rejections, and unexpected errors.

### Current limitations and safety boundaries

- **Read-only workspace access:** `Lattice.Desktop` registers only `FileSearchTool`, `ReadFileTool`, and `ListDirectoryTool` under `ToolPermissionPolicy.ReadOnlyOnly`. No file writes, patch application, or shell/command execution capabilities are registered.
- **Literal source-text search (not semantic resolution):** `find symbol <name>` performs case-insensitive substring matching over UTF-8 workspace files (scanning up to 100 matches and displaying up to 20 citations; skipping `.git`, `.vs`, `artifacts`, `bin`, and `obj`). It does **not** perform Roslyn or AST declaration/reference resolution.
- **File size and encoding bounds:** `inspect` and search skip or reject files larger than 262,144 bytes and reject invalid UTF-8 binary data rather than substituting replacement characters.
- **Narrow symbolic grammar:** Free-form natural language, multi-word symbol queries, and unsupported commands are rejected explicitly rather than guessed or silently converted into another operation.
- **Static symlink and reparse-point guard:** Workspace operations are confined to the explicitly connected root. Direct reads/directory resolution reject root-to-target paths containing symbolic links or reparse points, and directory/search traversal skips linked files and linked directories. This is a conservative **static** check at resolution/enumeration time and is **not** race-resistant (TOCTOU-resistant) against concurrent filesystem link replacement.
- **Untrusted workspace text:** Inspected file text and search snippets are treated purely as passive data and are never reparsed or executed as instructions.

## Solution layout

| Project | Purpose |
|---|---|
| `Lattice/` | `Lattice.Core` — UI-independent domain types, agent loop, workspace tools, safety boundary, and retrieval |
| `Lattice.Desktop/` | Desktop 0.1 executable host (`PhotinoX.Blazor` 5.3.5) and read-only workspace session UI |
| `Lattice.Desktop.Tests/` | Desktop session, composition, and rendered bUnit Blazor component tests |
| `Lattice.CSharp/` | Optional Roslyn syntax/semantic inspection and narrow syntax-edit library |
| `Lattice.Python/` | Optional Python AST syntax adapter |
| `Lattice.Cli/` | Limited controlled-statement console harness |
| `Lattice.Core.Tests/` | Core unit, safety-boundary, and regression corpus tests |
| `knowledge/`, `config/`, `tests/` | Example knowledge package, intent patterns, and test corpora |

## Documentation

- [Sequential Desktop 0.1 build plan](.agents/todo.md) — ordered phases, verification notes, and remaining manual GUI checks.
- [Windows desktop smoke test](docs/windows-smoke-test.md) — step-by-step manual Photino launch walkthrough.
- [Desktop product direction](docs/DESKTOP_DIRECTION.md) — product scope and first acceptance target.
- [Documentation index](docs/README.md)
- [Historical research-first specifications and milestone records](docs/archive/research-first/) — retained for provenance, not active instructions.
