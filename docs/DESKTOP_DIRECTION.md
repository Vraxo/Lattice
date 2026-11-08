# Lattice desktop direction

**Status:** approved product direction and active implementation brief  
**Replaces:** the research-first M0–M10 roadmap as the source of current priorities

## 1. The change

Lattice is not being scrapped or restarted. Keep the existing repository, history, Core, tools, adapters, fixtures, and tests.

What changes is the order of work:

- Stop treating architecture completion and research milestones as the product.
- Start assembling existing capabilities into a real desktop application.
- Prioritize demonstrable user workflows.
- Add abstractions only when a concrete workflow needs them.
- Keep each increment small, runnable, and independently verifiable.

The previous PRD, architecture, roadmap, ADRs, work queue, capability snapshot, and specialist design notes are preserved under [archive/research-first](archive/research-first/). They describe the earlier development approach and are not current instructions.

## 2. Product target

Lattice is a symbolic coding assistant for a local code workspace. It uses explicit structured state and bounded tools rather than an LLM. Its answers should be grounded in observable workspace evidence, and it should say when an operation is unsupported or ambiguous.

The initial product is a desktop application:

- C# 14 / .NET 10.
- Blazor UI hosted with PhotinoX.Blazor 5.3.5, following the existing Formloom implementation pattern.
- A new `Lattice.Desktop` executable project.
- Existing `Lattice.Core` remains framework-independent.
- UI state and desktop host concerns stay outside Core.

Do not restart the solution or replace existing components just to make the structure look cleaner.

## 3. First product slice: Lattice Desktop 0.1

The user should be able to:

1. Launch a desktop window.
2. Choose or enter a local workspace folder and explicitly connect to it.
3. See which workspace is active.
4. Enter a request in a simple conversation interface.
5. Inspect a file and see its actual contents.
6. Search for a symbol name and see matching source-text occurrences with file paths and line numbers.
7. See a clear result, no-result, unsupported-request, or error state.

The first supported requests are:

- `inspect <relative-path>`
- `find symbol <name>`

**Important accuracy boundary:** the current find operation is literal/case-insensitive source-text search using the existing bounded workspace search capability. It does not resolve C# declarations or references and must not be described as semantic symbol search. Roslyn semantic inspection is a separate existing library capability, not this first UI workflow.

The first release is read-only. No edits, patch application, arbitrary commands, shell access, or autonomous build/test execution.

## 4. Reuse what exists

Inspect and compose the existing components before introducing new ones. Relevant pieces include:

- `RequestInterpreter` and `IntentPatternCatalog`
- `WorkspaceQuestionAnswerer` and `EvidenceReference`
- `WorkspaceRoot` and `WorkspaceToolset`
- `FileSearchTool`, `ReadFileTool`, and `ListDirectoryTool`
- Existing tool registry, executor, and permission policy

The existing components are not all wired into the CLI. The new desktop composition root should connect the pieces needed for the first workflow without moving UI or application-specific policy into Core.

## 5. Workspace safety

- The user explicitly selects the workspace root.
- Reads and searches stay within that root.
- Keep the first desktop experience read-only.
- Before exposing workspace inspection, add conservative static protection against symlinks and reparse points: reject linked path components for direct reads and skip linked entries/directories while traversing.
- Document the limit honestly: this static check is not race-resistant against a link being replaced concurrently.
- Treat workspace contents as untrusted data, not instructions.

Do not add a permission for writes or command execution in this slice.

## 6. Implementation order

Work in small checkpoints. Keep the app runnable after each checkpoint.

### Checkpoint A — real desktop inspection

Create the desktop host and a minimal but functional window. Let the user connect a workspace and run `inspect <relative-path>` through the existing workspace read path. Show actual file content, clear errors, and the active workspace.

Do not submit an empty desktop shell as the milestone.

### Checkpoint B — grounded source search

Wire `find symbol <name>` through the existing bounded request interpretation and workspace question-answering path. Display each matching path and line number. Label it as source-text search, not semantic resolution.

### Checkpoint C — usable conversation flow

Keep prior requests and responses visible during the session. Display unsupported input, ambiguity, no matches, and file-access errors explicitly. Keep evidence visible alongside the answer.

These checkpoints can be separate small changes. Do not bundle unrelated refactoring.

## 7. Verification

For every checkpoint:

- Build the solution in Release and run the existing test suite.
- Add focused tests for the new host/workflow. Use bUnit for Blazor interactions where appropriate.
- Use temporary fixture workspaces for file operations.
- Cover workspace connection, successful inspection, matching and non-matching searches, missing paths, unsupported requests, traversal attempts, and static link/reparse-point rejection.
- Confirm the desktop project launches and perform a short manual walkthrough. Clearly distinguish automated tests from a manually observed run.
- Preserve the existing Core tests and report exact command output/counts only when actually observed.

The acceptance target for the first visible product checkpoint is straightforward: a running desktop window connected to a real repository, a successful file inspection, a successful source-text search, and visible path/line evidence — all read-only.

## 8. Explicitly not the next task

Do not resume M9/M10 or introduce embeddings, semantic retrieval, broad natural-language understanding, general autonomous planning, or a universal capability framework.

Do not implement file editing, patch preview/application, arbitrary shell commands, or build/test command execution as part of Desktop 0.1. A later, separately scoped increment may add a narrow edit-preview flow with explicit user approval after the read-only product is working.

**The priority is a working application that can be used and judged.**


## 9. Desktop 0.2 — conversational interaction

Desktop 0.1 proved the workspace operations but exposed them as explicit request commands. The next active increment changes the primary interaction to chat.

The user should be able to write ordinary, supported phrases such as:

- "Where is WorkspaceRoot used in this project?"
- "Can you find where WorkspaceRoot is used throughout this project?"
- "Show me Lattice/WorkspaceRoot.cs."
- "What's in Lattice/WorkspaceRoot.cs?"

The interface should display separate user and assistant messages, a multiline composer, and file/search evidence inline with the assistant reply. The workspace selector remains available but compact.

Language interpretation remains deterministic and bounded. It may support a single captured value surrounded by known prefix/suffix literals; it must preserve ambiguity instead of guessing. It is not general English comprehension and does not use an LLM.

A minimal conversational follow-up such as "Show me that file" may resolve only when the immediately relevant preceding turn identifies one unambiguous file. Otherwise Lattice asks which file the user means.

The original Desktop 0.2 assignment is preserved in [the archive](../.agents/archive/desktop-0.2.md).


## 10. Desktop 0.3 — workspace file discovery and charcoal styling

User testing of Desktop 0.2 exposed an important gap: a natural file request such as "Show me Program.cs" should not be interpreted as a root-only relative path. If the exact relative path is omitted, Lattice should find matching basenames throughout the connected workspace.

Resolution rules are strict and predictable:
- An exact relative path takes precedence and opens that exact file.
- A bare filename is searched by basename within the connected root.
- One match opens directly.
- Multiple matches are shown as full relative paths and require the user to select one. Lattice must never guess.
- No match produces a friendly workspace-wide not-found response.

Filename discovery is distinct from searching file contents. It must reuse the safe workspace traversal boundary and preserve root confinement, link/reparse-point exclusion, and read-only behavior.

Desktop 0.3 also replaces the blue-black appearance with a neutral charcoal palette: gray backgrounds and surfaces, subtle gray borders, readable off-white text, and restrained neutral controls/focus treatment. This is a presentation-only change.


## 11. Desktop 0.4 — C# structure and declaration navigation

The first C# code-understanding slice reuses the existing `Lattice.CSharp` Roslyn dependency and exposes syntax declaration facts in the desktop conversation.

Supported requests include:

- "What classes are in this project?"
- "What types are in this project?"
- "Where is WorkspaceRoot defined?"
- "Show me the members of WorkspaceRoot."

The workspace inspector reads C# files through the existing `ListDirectoryTool` and `ReadFileTool` under the read-only permission policy. It skips generated/build directories and has explicit directory, depth, source-file, total-byte, and declaration bounds. Results include the declaration kind, signature, relative path, and line/column.

**This is not semantic project analysis.** The inspector parses syntax trees, but it does not load MSBuild projects, resolve NuGet/project references, compile the workspace, identify call sites, resolve overloads, or prove that a declaration is the target of a given reference. Literal source-text search remains a separate capability.

The detailed scope and acceptance gates are in the [active Desktop 0.4 checklist](../.agents/todo.md).
