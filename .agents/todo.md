# Lattice Desktop — Active Work Queue

**Status:** not started  
**Product target:** Lattice Desktop 0.1  
**Source of truth:** `docs/DESKTOP_DIRECTION.md`

This is the active, sequential implementation checklist. It replaces the archived research-first M0–M10 work queue.

## How to work this queue

- Begin with **Task 1**, the first unchecked task. Do not jump ahead.
- Complete one task at a time. Do not start the next task in the same pass.
- Check a box only after the implementation exists and the stated verification passed. Never check work off because it is planned, partly implemented, or believed to be covered.
- Each task should be a small, independently buildable and testable increment. Keep the app runnable after each task.
- Before editing, inspect `git status`, relevant code, and the actual existing APIs. Preserve user changes.
- Reuse existing code wherever possible. Do not add speculative frameworks, broad refactors, or new architecture documents.
- For each task, run focused tests plus the full Release build/test suite when practical. Report actual commands and results; never invent test counts.
- Do not commit. The user handles commits.
- When a task is complete, update its checkboxes and status, report changed files, behavior, verification, limitations, and a suggested commit message, then stop.
- If an implementation detail is routine and reversible, decide and proceed. Ask only when genuinely blocked.
- Do not add LLMs, embeddings, network access, arbitrary shell execution, automatic writes, or background services.

## Task 1 — Protect workspace traversal from links

**Goal:** enforce the approved conservative static symlink/reparse-point boundary before the desktop exposes workspace reads or searches.

- [ ] Inspect the existing `WorkspaceRoot`, shared workspace file-access code, and file tools. Confirm where path confinement and enumeration currently occur; do not duplicate path-security logic.
- [ ] Reject a symlink/reparse-point in any path component used for direct file or directory access.
- [ ] Skip symlink/reparse-point files and directories during recursive search/list traversal; do not follow them outside the selected workspace.
- [ ] Add focused tests for direct file access, linked parent directories, linked search entries, and linked subdirectories. Make platform-dependent link creation explicit and skip only when the platform cannot create the fixture.
- [ ] Verify existing workspace tests and the full Release build/test suite.

**Acceptance:** direct access cannot traverse a link, recursive workspace tools do not follow links, normal files continue to work, and the documented boundary is explicitly static rather than race-resistant.

**Status:** pending.

## Task 2 — Add a real desktop host and workspace connection

**Goal:** create the actual PhotinoX/Blazor application, not another console demo.

- [ ] Inspect Formloom's PhotinoX.Blazor 5.3.5 host pattern and the current Lattice solution/project conventions. Reuse the proven pattern; do not copy unrelated Formloom code.
- [ ] Add `Lattice.Desktop` targeting .NET 10 with PhotinoX.Blazor 5.3.5. Keep UI/desktop dependencies outside `Lattice.Core`.
- [ ] Add a simple desktop window with a workspace path field and an explicit Connect/Open action. Entering or pasting a path is sufficient; do not depend on an unavailable OS folder-picker API.
- [ ] Make Connect perform real directory/root validation and show the active workspace or a useful error. Do not present a path as connected merely because it was typed.
- [ ] Add focused tests for workspace connection success/failure and a Blazor interaction test where appropriate.
- [ ] Verify the solution builds and tests in Release, and verify the desktop process launches. Report separately what was automated and manually launched.

**Acceptance:** a running desktop window can connect to a real local directory and visibly show which workspace is active. No inert mock controls.

**Status:** pending.

## Task 3 — Make file inspection work in the desktop UI

**Goal:** deliver the first useful end-to-end workspace interaction.

- [ ] Add the `inspect <relative-path>` request to the desktop conversation/input flow.
- [ ] Route it through the existing workspace read capability and the Task 1 link-safety boundary. Reuse existing Core tools/services instead of implementing a second file reader in the UI.
- [ ] Display actual file content with its relative path and useful line information. Handle missing files, directories, invalid paths, unreadable/unsupported encodings, and attempts to escape the workspace with clear results.
- [ ] Keep all workspace operations read-only. Do not register or expose write or command-execution capabilities.
- [ ] Add fixture-based service and UI tests for successful inspection and representative failures.
- [ ] Verify Release build/tests and run the desktop against a real repository.

**Acceptance:** the user can connect a real workspace, enter `inspect <relative-path>`, and see the actual contents from that file in the desktop window.

**Status:** pending.

## Task 4 — Add grounded source-text search

**Goal:** let users find symbol-name occurrences and inspect the evidence.

- [ ] Add `find symbol <name>` using existing `RequestInterpreter`, `WorkspaceQuestionAnswerer`, `WorkspaceToolset`, and/or search APIs where they fit.
- [ ] Show each result with its relative file path, line number, and matching source line. Keep results bounded by existing tool limits.
- [ ] Label the operation accurately as **source-text search**. Do not claim it resolves declarations, references, overloads, or semantic C# symbols.
- [ ] Show a useful no-results state and reject unsupported or ambiguous requests rather than guessing.
- [ ] Add tests for matching, no matches, multiple files, limits, unsupported input, ambiguity, and link traversal.
- [ ] Verify Release build/tests and manually demonstrate the request against a real repository in the desktop app.

**Acceptance:** a user can enter `find symbol WorkspaceRoot` and see actual matching paths and line numbers, with the source-text limitation clearly stated.

**Status:** pending.

## Task 5 — Finish the first usable conversation flow

**Goal:** make the first workflows understandable and demonstrable as one small application.

- [ ] Keep requests and responses visible as a conversation/session rather than replacing the previous result on every action.
- [ ] Present inspection content and search evidence clearly, without hiding paths, line numbers, errors, or limitations.
- [ ] Make empty input, unsupported requests, ambiguity, no results, workspace disconnection, and file-access errors understandable.
- [ ] Add focused UI tests for conversation/result states; preserve the existing Core suite.
- [ ] Update the README with the actual desktop launch command, supported requests, and current limitations.
- [ ] Complete a manual walkthrough: launch, connect to the Lattice repository, inspect a real source file, and search for `WorkspaceRoot`. Record what was actually observed.
- [ ] Run the full Release build/test suite and report exact results.

**Acceptance:** Lattice Desktop 0.1 is a working, read-only desktop coding assistant for workspace inspection and source-text search. The user can reproduce the demonstration from the README.

**Status:** pending.

---

## Deferred — not part of Desktop 0.1

Do not turn these into tasks until the first desktop workflow is working and the user asks to proceed:

- Edit proposals, diff preview, and user-approved patch application.
- Build/test command execution or any arbitrary shell access.
- Roslyn-backed semantic symbol/reference navigation in the UI.
- Broader natural-language understanding or autonomous multi-step planning.
- Web, HTTP/API, database, MCP, embedding, or remote-tool integrations.
- Reopening the former M9/M10 research milestones.
