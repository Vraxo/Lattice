# Lattice — Desktop 0.4: C# Structure & Declaration Navigation

> **Status:** approved and implemented on `assistant/desktop-0.4-csharp-structure`; Windows CI verification is pending.
>
> **Baseline:** Desktop 0.3 is merged in PR #13. The 0.3.1 extensionless filename refinement is in [PR #14](https://github.com/Vraxo/Lattice/pull/14), currently open with green Windows CI. This branch is stacked on PR #14 so the two scopes remain independently reviewable.
>
> **Deliverable:** give Lattice a useful first layer of C# code understanding: list types/classes, locate actual declarations, inspect a type's declared members, and show path/line/signature evidence in the conversation.

## Product intent

Lattice should begin understanding the *structure* of C# source, not merely matching words in files.

Supported conversational requests:

- "What classes are in this project?"
- "What types are in this project?"
- "Where is WorkspaceRoot defined?"
- "Where is WorkspaceRoot declared?"
- "Show me the members of WorkspaceRoot."
- "What members does WorkspaceRoot have?"

These operations use Roslyn syntax trees and report declarations that are actually present in C# source files. Each result includes a relative path, line, declaration kind, and readable signature.

**Accuracy boundary:** this milestone is syntax-based structure discovery only. It does not load MSBuild projects, resolve NuGet/project references, compile the workspace, identify call sites, resolve overloads, or find semantic references. Keep the existing literal source-text search clearly separate. Do not describe a declaration listing as a reference search or claim that the whole project compiles.

## Safety and resource bounds

- Use `ListDirectoryTool` and `ReadFileTool` through the existing `ToolExecutor` and `ToolPermissionPolicy.ReadOnlyOnly`; do not add a separate direct filesystem traversal in the UI.
- Continue to rely on workspace-root confinement and static symlink/reparse-point rejection in the existing tools.
- Skip `.git`, `.vs`, `artifacts`, `bin`, and `obj`.
- Bound work to 512 directories, depth 32, 200 successfully read C# source files (500 examined), 8 MiB total source text, and 1,000 declarations. Each file read retains the existing 262,144-byte limit.
- Report when the scan is truncated, when files could not be read, or when syntax errors were encountered. Roslyn error recovery may still provide useful declarations, but must not be represented as a successful project compilation.
- Treat all workspace contents as untrusted data. Render declarations as encoded text. Do not execute or interpret comments, strings, or source contents as instructions.
- The whole feature remains read-only.

## Definition of done

### Declaration inventory
- "What classes are in this project?" lists class and record declarations with relative path, line, and signature.
- "What types are in this project?" also includes structs, interfaces, enums, and delegates.
- Non-C# files and generated/build directories do not appear in the result.

### Definition lookup
- "Where is WorkspaceRoot defined?" finds matching declarations, not arbitrary text mentions.
- Multiple declarations (for example partial declarations or same-name types) are all shown; no arbitrary candidate is chosen.
- A single-file result supports the existing narrow follow-up "Show me that file." Multiple result files require explicit selection.

### Member inspection
- "Show me the members of WorkspaceRoot." lists directly declared constructors, methods, properties, fields, events, indexers, and operators for matching types.
- It does not mix in members from a different type just because a source-text token happens to match.
- Empty and missing results receive clear plain-language responses.

### UI, tests, and verification
- Declarations render as a distinct result type, not as literal source-text search evidence.
- The UI identifies the results as Roslyn syntax declarations and states the semantic limitations.
- Automated tests cover inventory, class/type filtering, definitions, members, follow-up file opening, missing declarations, generated directory exclusion, unreadable source files, and bUnit rendering.
- Existing file lookup, file reading, source-text search, theme, and follow-up behavior remain intact.
- Windows/.NET 10 restore, Release build, and the full test suite pass in PR CI.
- Native Photino window launch remains a manual user check; do not claim it was observed by CI.

## Working rules

1. Read this file, `AGENTS.md`, `README.md`, and `docs/DESKTOP_DIRECTION.md` before editing.
2. Preserve PR #14 as the base dependency. Do not merge or rewrite it from this branch.
3. Keep this increment on `assistant/desktop-0.4-csharp-structure`; do not commit to main or force-push.
4. Reuse the existing `Lattice.CSharp` Roslyn adapter and the Core workspace tools. Do not add another parser or unsafe filesystem scanner.
5. Keep syntax-only structure inspection distinct from source-text search and semantic reference resolution.
6. Do not introduce LLMs, embeddings, project builds, package restore, network access, source edits, or command execution.
7. Use Windows/.NET 10 PR CI for verification. Fix failures rather than weakening tests.
8. Leave actual Photino GUI checks pending for the user.
9. Open one draft PR targeting `assistant/filename-stem-lookup` (PR #14's branch). Once PR #14 is merged, retarget this PR to main. Do not merge either PR.
10. Complete the checklist and report the PR link, exact CI run/results, remaining manual checks, and launch instructions.

---

# Phase 0 — Inspect the existing implementation

- [x] Inspect the current stacked base, PR #14, and CI workflow.
- [x] Review `Lattice.CSharp` syntax and semantic inspectors and identify the limits of the existing semantic compilation helper.
- [x] Trace safe recursive directory listing and UTF-8 reads through `ListDirectoryTool`, `ReadFileTool`, and `ToolExecutor`.
- [x] Confirm the desktop chat's phrase catalog, request processor, session context, result model, and rendered evidence patterns.
- [x] Choose syntax-only structure discovery as the first product step; defer project-aware semantic binding.

**Done when:** the implementation uses the existing safe workspace tools and makes no claim beyond parsed C# declarations.

# Phase 1 — Bounded C# declaration discovery

- [x] Add a workspace structure inspector to `Lattice.CSharp`.
- [x] Traverse directories using `ListDirectoryTool` under read-only policy; skip generated/build directories and respect depth/directory limits.
- [x] Read `.cs` files using `ReadFileTool` under read-only policy; retain its UTF-8 and per-file size protections.
- [x] Enforce source-file, total-byte, and declaration limits; return truncation and skipped-file information.
- [x] Parse syntax with Roslyn and collect type declarations plus direct class/struct/interface/record members.
- [x] Include relative path, line/column, namespace, containing type, kind, and signature for each declaration.
- [ ] Verify symlink/reparse-point behavior and resource limits in Windows CI.

# Phase 2 — Conversational structure queries

- [x] Add deterministic patterns for listing types/classes, finding a definition/declaration, and listing members of a named type.
- [x] Route those patterns to the C# structure inspector without changing existing file/search routing.
- [x] Display an explicit no-match result when a declaration is absent; do not fall back to literal text matching.
- [x] Carry the relevant source file(s) into the existing narrow follow-up context so "Show me that file" works when unambiguous.
- [x] Preserve exact user messages and normal chronological conversation history.
- [ ] Verify phrase ambiguity and request regressions in Windows CI.

# Phase 3 — UI, tests, and documentation

- [x] Render declaration evidence separately from source-text search evidence.
- [x] Show declaration kind, signature, relative path, and line number.
- [x] State clearly that the results are syntax-based and do not compile projects or resolve references.
- [x] Add tests for type inventory, definitions, member listings, missing definitions, unreadable files, excluded directories, follow-up context, and rendered UI.
- [ ] Add any additional tests required by CI findings.
- [ ] Update README, desktop direction, and Windows smoke-test steps.
- [ ] Inspect the full diff for unintended capabilities, unsafe access, and misleading semantic claims.
- [ ] Run Windows/.NET 10 Release restore/build/test in PR CI and record exact per-project results.

---

## STOP — one stacked draft PR for review

1. Open one draft PR targeting `assistant/filename-stem-lookup` (the branch behind PR #14).
2. Keep changes on this branch and fix all CI failures.
3. Leave the PR as a draft until automated verification is green; then mark it ready for review.
4. Do not merge. The user should review PR #14 first; after that merges, retarget this PR to main.
5. Report exact tests and any remaining manual GUI checks. Stop there.
