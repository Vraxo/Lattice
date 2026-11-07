# Instructions for coding agents

## Active source of truth

Read these files first, in this order:

1. `README.md`
2. `docs/DESKTOP_DIRECTION.md`
3. `.agents/todo.md`

**The active execution queue is `.agents/todo.md`.** Start at its first unchecked task, follow the tasks in order, and check items off only after their acceptance criteria and verification pass. Do not revive the archived M0–M10 roadmap.

Everything under `docs/archive/research-first/` is historical project material. Do not treat its PRD, architecture document, M0–M10 roadmap, decision log, work queue, capability snapshot, or old AI work protocol as current marching orders. Do not continue the old research-first milestones unless the user explicitly reopens one.

## Current objective

Build Lattice as a usable desktop application, product-first and in small, independently verifiable vertical increments. The immediate goal is the first real desktop workflow described in `docs/DESKTOP_DIRECTION.md`, not another planning or architecture phase.

- Preserve the existing Core, tools, adapters, tests, and repository history.
- Add a `Lattice.Desktop` host using .NET 10, Blazor, and PhotinoX.Blazor 5.3.5, following the working pattern already used by Formloom.
- Keep UI and desktop dependencies out of `Lattice.Core`.
- Compose existing capabilities before inventing replacements.
- A shell with controls that do nothing is not a deliverable. Connect the interface to actual workspace operations.
- Prefer a small working vertical slice over speculative abstractions, broad refactors, benchmarks, or new architecture documents.
- Do not add an LLM, embeddings, network features, arbitrary shell execution, background services, or automatic file writes.

## First user-visible workflow

The first desktop slice should let the user choose a local workspace, inspect a file, and search for a requested symbol name with visible file-and-line evidence. The initial `find symbol` operation is bounded source-text search, **not** semantic symbol resolution. State that limitation clearly in the UI and documentation.

Reuse the existing request interpretation, workspace question-answering, workspace-root, file-tool, and evidence components where they fit. Do not duplicate them in the Desktop project.

Workspace access must remain read-only and confined to the selected root. Add the previously agreed static symlink/reparse-point protection before exposing workspace inspection: reject linked path components for direct access and skip linked entries/directories during traversal. This is a conservative static check, not a claim of race-resistant protection against concurrent filesystem changes.

## Implementation and verification

- Inspect the current code and Git status before changing files. Do not overwrite user changes.
- Keep each increment buildable and runnable; do not stop at an empty scaffold when a real interaction can be connected.
- The first unchecked task in `.agents/todo.md` is the only active task. Complete it, update its checkboxes/status, report the result, and stop; do not skip ahead or implement later tasks in the same pass.
- Make the smallest change that produces observable user value. Avoid unrelated cleanup and new abstractions without a concrete need.
- Add focused tests for new behavior and UI interactions; preserve the existing Core test suite.
- Run the real restore/build/test commands and report their actual outcomes. Never invent test counts or claim that a GUI was manually launched when it was not.
- For a desktop checkpoint, include exact launch steps and a short manual demonstration using a real workspace.
- Do not commit automatically. Report changed files, verification, limitations, and one suggested commit message; the user handles commits.
- Do not ask for approval for routine, reversible implementation details. If a genuine blocker makes progress impossible, ask one direct question.

## Stop condition

After the first working desktop checkpoint, stop and report what can actually be demonstrated. Do not automatically proceed to editing, command execution, autonomous planning, or later capabilities.
