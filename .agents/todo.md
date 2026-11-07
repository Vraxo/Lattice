# Lattice — Desktop 0.1 Build Plan

> **Status:** not started. This is the active, sequential implementation checklist.
>
> **First deliverable:** a runnable Lattice desktop application that connects to a local code workspace, inspects a real file, and finds source-text occurrences with visible file-and-line evidence.
>
> **Checkpoint:** Complete this plan, then stop and report back for review. Do not continue into editing, command execution, autonomous planning, or a broader roadmap without a new direction.

## Project intent

Lattice is a symbolic, non-LLM coding assistant. Its job is to work from explicit state, bounded tools, and inspectable evidence — not to imitate a generative model by accumulating speculative architecture.

This first product slice should prove that the useful existing Core capabilities can be composed into a real desktop workflow. Prefer working software, visible results, and small changes over new frameworks, abstractions, or research milestones.

The existing Core, adapters, tools, fixtures, tests, and Git history are retained. The desktop host is a new composition root; it must not pull UI dependencies into Core.

## Definition of done

Desktop 0.1 is complete only when:

- Lattice launches as a real PhotinoX.Blazor desktop window.
- The user can enter a local folder path, connect to it explicitly, and see the active workspace.
- The app can inspect a file within that workspace and display its real contents.
- The app can search for a symbol name and display actual matching source-text occurrences with file paths and line numbers.
- The interface keeps the request/result history visible and communicates errors, unsupported input, and no matches clearly.
- The UI explicitly describes the initial symbol search as literal/source-text search, not semantic declaration/reference resolution.
- Workspace access is confined to the selected root, includes the static symlink/reparse-point safeguards below, and remains read-only.
- Automated tests cover the workspace workflow and relevant UI interactions; the existing Core suite remains passing.
- The README documents actual prerequisites, launch commands, supported requests, and limitations.
- The app has been launched and the complete workflow has been demonstrated against a real repository. A CI build is not a substitute for a claimed manual GUI launch.

## Technology

Use the following unless an actual compatibility/build issue requires a documented adjustment:

- C# 14 / .NET 10 (net10.0).
- Blazor hosted by **PhotinoX.Blazor 5.3.5**, following the existing Vraxo/Formloom desktop-host pattern.
- A new Lattice.Desktop executable project.
- Existing Lattice.Core remains UI-framework-independent.
- Keep tests in the existing xUnit setup. Add bUnit only where it provides meaningful coverage for rendered Blazor interactions.
- Pin dependencies consistently with the repository's existing conventions. Do not introduce unrelated packages or infrastructure.

## Working rules

1. Read this file fully before coding. Start with Phase 0 and work through the phases in order. Do not skip ahead.
2. Complete one small phase at a time. Each phase must build and test before moving to the next.
3. Inspect the actual code before changing it. Reuse existing components where practical; do not duplicate the workspace tools or question-answering logic in Desktop.
4. Keep the app runnable and interactive after each desktop phase. An empty window, static mock-up, or control that does nothing is not a deliverable.
5. Avoid unrelated cleanup, speculative abstractions, general-purpose frameworks, and changes to old research milestones.
6. Do not add an LLM, embeddings, web/network access, telemetry, background services, arbitrary shell execution, automatic downloads, or automatic file writes.
7. Inspect Git status before editing and preserve user changes. Never reset or rewrite existing commits.
8. Do not commit. Report a suggested commit message; the user commits after review.
9. Never claim a build, test, manual launch, or test count that was not actually observed. If a command cannot run locally, use the repository's existing CI path where practical and report the evidence precisely.
10. Update these checkboxes only when their stated work and verification are complete. A partially completed item stays unchecked; explain the partial state in the phase notes.
11. Do not ask for approval for routine, reversible implementation details. Ask one direct question only if a genuine blocker prevents progress.

---

# Phase 0 — Establish the real baseline

### 0.1 Inspect the current repository and environment

- [ ] Inspect the root, solution, project references, build configuration, README, active instructions, and current Git status.
- [ ] Confirm the existing names and locations of Core workspace components, including WorkspaceRoot, WorkspaceToolset, FileSearchTool, ReadFileTool, WorkspaceQuestionAnswerer, RequestInterpreter, and evidence types. Do not assume the archived capability matrix is perfectly current.
- [ ] Inspect the Formloom desktop host as a reference for PhotinoX.Blazor composition and project setup. Reuse the proven pattern selectively; do not copy Formloom-specific rendering or scene code.
- [ ] Check dotnet --info and the installed SDK/runtime. Do not retarget the solution just because a local SDK is missing.

**Done when:** The agent reports the actual solution shape, reusable components, current Git status, and available build environment before making code changes.

### 0.2 Run and record the existing baseline

- [ ] Run restore, Release build, and the existing test suite using the repository's actual solution and commands.
- [ ] Record exact command results, warnings/errors, and the observed test counts. Do not reuse old counts from the archived work queue.
- [ ] If the local environment lacks the .NET SDK, record the exact failure and use GitHub Actions CI for verification where practical. Do not install or change SDK infrastructure without a concrete need.

**Done when:** The existing baseline is honestly recorded, and any pre-existing failures are distinguished from changes introduced by this plan.

---

# Phase 1 — Create a real desktop host

### 1.1 Add the Desktop project

- [ ] Create Lattice.Desktop as a .NET 10 executable using PhotinoX.Blazor 5.3.5.
- [ ] Add it to Lattice.slnx.
- [ ] Reference Core only where the host needs it. Keep Photino, Blazor, and desktop-specific packages out of Lattice.Core.
- [ ] Follow the working Formloom host pattern for startup, root component registration, static assets, and window lifecycle. Keep the initial structure small.
- [ ] Verify the project restores and the full solution builds.

**Done when:** The repository contains a real desktop executable project and the solution builds; Core remains independent of UI/desktop dependencies.

### 1.2 Build the first useful window

- [ ] Add a simple, usable desktop layout with Lattice identity, a workspace-path field, an explicit Connect action, a visible connected-workspace indicator, and an area for requests/results.
- [ ] Make Connect validate that the entered path exists and is a directory; show a clear error for invalid input.
- [ ] Keep connection state in a small application/session service only if needed by both UI and tests. Do not add a general session framework.
- [ ] Add focused tests for workspace connection state and rendered UI behavior where practical.
- [ ] Launch the desktop app and verify that Connect actually changes the displayed active workspace.

**Done when:** A user can open the window, enter a real local folder, connect, and see which workspace is active. This is the first visible checkpoint; do not stop with a static mock-up.

> **Phase 1 note:** Record exact launch commands and distinguish an automated build from a manually observed desktop launch.

---

# Phase 2 — Protect workspace access before exposing file contents

### 2.1 Inspect and implement static link/reparse-point checks

- [ ] Trace all direct-read and recursive-search path handling from the UI-facing composition down to the shared workspace file-access boundary.
- [ ] Implement conservative static symlink/reparse-point protection at the shared workspace access boundary rather than adding UI-only checks.
- [ ] Reject direct file access when the root-to-target path contains a symlink or reparse-point component.
- [ ] During recursive search/list traversal, skip linked files and directories rather than following them.
- [ ] Preserve ordinary in-root file access and existing lexical traversal protection.
- [ ] Keep the threat-model statement explicit: this is a static check and is **not** race-resistant against a link being replaced concurrently.

### 2.2 Test the safety boundary

- [ ] Add tests for a normal file under the workspace root.
- [ ] Add tests for .. traversal and rooted/absolute path attempts.
- [ ] Add tests for a linked file and a linked directory where the platform supports creating them; skip only platform-specific cases that genuinely cannot be created, and explain the limitation.
- [ ] Verify linked entries are not read or traversed and that ordinary files still work.
- [ ] Verify the first Desktop composition still grants read-only access; no write or command capability is registered.

**Done when:** Existing workspace reads/searches remain functional, linked paths cannot be followed by the static checks, the behavior is test-covered, and no write/command path is exposed.

---

# Phase 3 — Inspect actual workspace files from the desktop

### 3.1 Connect the Inspect request

- [ ] Implement the first user request: inspect followed by a relative path.
- [ ] Route it through the existing workspace/read components (especially ReadFileTool and the established workspace access path) instead of creating a second file-reading implementation.
- [ ] Use the explicitly connected workspace root; never interpret a user-supplied path as permission to leave it.
- [ ] Display the selected relative path and the actual file contents in a readable, scrollable result area.
- [ ] Handle missing files, directories supplied instead of files, oversized/unsupported files, access errors, and invalid paths without crashing.
- [ ] Keep the operation read-only and treat file contents as untrusted data, never as instructions.

### 3.2 Verify inspection end to end

- [ ] Add tests for successful inspection, missing file, invalid path, root escape, and static link rejection.
- [ ] Add UI/session tests proving the request reaches the real reader and that returned content/errors appear in the interface.
- [ ] Run the focused tests, Release build, and full existing suite.
- [ ] Manually launch the desktop app and inspect a real source file from the Lattice repository.

**Done when:** A user can connect a real repository, enter an inspect request for an existing relative path, and see the actual file contents in the desktop window.

---

# Phase 4 — Find symbols and show grounded evidence

### 4.1 Connect the Find request

- [ ] Implement the first search request: find symbol followed by a name.
- [ ] Reuse RequestInterpreter, IntentPatternCatalog, WorkspaceQuestionAnswerer, WorkspaceToolset, and FileSearchTool where their current contracts fit.
- [ ] Search source text using the existing bounded, case-insensitive search behavior. Do not claim C# semantic symbol resolution or declaration/reference grouping.
- [ ] Display each match with its relative file path, line number, and matching line/snippet.
- [ ] Show a clear no-matches state. Do not silently convert unsupported or ambiguous requests into a different operation.
- [ ] Label this feature in the UI as **source-text search** (not semantic symbol search).

### 4.2 Verify search and evidence

- [ ] Add fixture tests for multiple matches, one match, no matches, excluded/oversized files, and unsupported or ambiguous input.
- [ ] Add UI/session tests proving that actual evidence paths and line numbers are rendered.
- [ ] Verify that all returned evidence belongs to the connected workspace and that search cannot follow linked entries.
- [ ] Run focused tests, Release build, and the full existing suite.

**Done when:** A user can enter a find-symbol request in a connected Lattice checkout and see actual matching file paths and line numbers.

---

# Phase 5 — Finish the first usable read-only workflow

### 5.1 Make the conversation usable

- [ ] Keep prior user requests and Lattice responses visible during the current session.
- [ ] Present file content and search evidence clearly, without hiding the source path/line information.
- [ ] Show distinct states for unsupported input, ambiguous input, no results, invalid workspace, missing file, and unexpected errors.
- [ ] Prevent overlapping submissions from corrupting the visible session state.
- [ ] Keep the interface focused on workspace inspection; do not add editing, autonomous planning, or arbitrary command execution.

### 5.2 Complete automated verification and documentation

- [ ] Add/complete focused UI component tests for Connect, Inspect, Find, visible evidence, empty results, and error states.
- [ ] Run dotnet restore, Release build, and the full test suite; report exact observed results.
- [ ] Review the final diff for unrelated refactors, accidental dependencies, unsafe path handling, and inflated capability claims.
- [ ] Update the README with the desktop prerequisites, exact launch command, supported request forms, and honest source-text-search/read-only limitations.
- [ ] Ensure the solution/CI workflow builds and tests the Desktop project.
- [ ] Confirm the desktop application launches and manually demonstrate Connect → Inspect → Find against a real repository.

**Done when:** Desktop 0.1 satisfies every item in the Definition of done above.

---

## STOP — report the first product checkpoint

Once all applicable tasks above are complete, stop. Do not select or begin a second milestone.

The completion report must include:

1. The phases completed and any unchecked items.
2. Files changed and a concise explanation of each.
3. Exact restore/build/test commands and observed results.
4. Actual test counts, not estimates.
5. Exact desktop launch command and whether the GUI was manually launched.
6. A concise demonstration of Connect → Inspect → Find, including example evidence.
7. Known limitations and any platform-specific verification gaps.
8. Acceptance gate: PASS or FAIL, with evidence.
9. One suggested commit message.

## Explicitly out of scope

- General natural-language understanding or unrestricted conversation.
- LLMs, embeddings, model downloads, training, or generation.
- Semantic symbol/reference navigation in the first search implementation.
- Editing, patch preview/application, or automatic writes.
- Shell or arbitrary command execution.
- Autonomous multi-step planning or unattended task completion.
- Web search, MCP, network tools, telemetry, or background services.
- Resuming the archived M0–M10 research-first roadmap.
