# Instructions for coding agents

## Read first

Read these files in order:

1. README.md
2. docs/DESKTOP_DIRECTION.md
3. .agents/todo.md — the active sequential implementation checklist

The code and tests define what currently exists. The desktop direction defines the product target. The TODO file defines the ordered implementation tasks and acceptance gates.

**Start implementation from .agents/todo.md.** Read it fully, begin at the first unchecked task, and work through the phases in order. Update its checkboxes only when the stated work and verification are complete. Do not skip ahead or invent a new roadmap.

Everything under docs/archive/research-first/ is historical project material. Do not treat its PRD, architecture document, M0–M10 roadmap, decision log, work queue, capability snapshot, or old AI work protocol as current marching orders. Do not resume those milestones unless the user explicitly reopens them.

## Current objective

Build Lattice as a usable desktop application, product-first and in small, independently verifiable vertical increments.

- Preserve the existing Core, tools, adapters, tests, fixtures, and repository history.
- Add a Lattice.Desktop host using .NET 10, Blazor, and PhotinoX.Blazor 5.3.5, following the working pattern already used by Formloom.
- Keep UI and desktop dependencies out of Lattice.Core.
- Compose existing capabilities before inventing replacements.
- A shell with controls that do nothing is not a deliverable. Connect the interface to actual workspace operations.
- Prefer a small working vertical slice over speculative abstractions, broad refactors, benchmarks, or new architecture documents.
- Do not add an LLM, embeddings, network features, arbitrary shell execution, background services, or automatic file writes.

## First user-visible workflow

The first desktop slice lets the user connect a local workspace, inspect a file, and search for a requested symbol name with visible file-and-line evidence. The initial find operation is bounded source-text search, **not** semantic symbol resolution. State that limitation clearly in the UI and documentation.

Reuse the existing request interpretation, workspace question-answering, workspace-root, file-tool, and evidence components where they fit. Do not duplicate them in the Desktop project.

Workspace access must remain read-only and confined to the selected root. Add the static symlink/reparse-point protection described in .agents/todo.md before exposing workspace inspection. This is a conservative static check, not a claim of race-resistant protection against concurrent filesystem changes.

## Implementation rules

- Inspect the current code and Git status before edits. Do not overwrite user changes.
- Keep the app runnable and interactive as it is built. Avoid empty scaffolds and static mock-ups.
- Work through the TODO phases in order. Do not ask for approval for routine, reversible implementation details.
- Keep each phase cohesive and independently verifiable. Do not bundle unrelated refactors.
- Add focused tests for new behavior and preserve the existing Core test suite.
- Run actual restore/build/test commands and report their real outcomes. Never invent test counts or claim a GUI was launched when it was not.
- If local tooling is unavailable, report the exact failure and use the repository's CI path where practical.
- Do not commit automatically. Report the changed files, verification, limitations, and one suggested commit message; the user handles commits.
- If a genuine blocker prevents safe progress, explain it and ask one direct question rather than silently changing scope.

## Stop condition

Complete the Desktop 0.1 plan in .agents/todo.md, then stop and report the acceptance result. Do not automatically begin editing, command execution, autonomous planning, or a later milestone.
