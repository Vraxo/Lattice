# Lattice Desktop 0.3 — Archived Assignment

> **Status:** implementation merged in PR #13; extensionless filename-stem refinement is in PR #14 (open, CI passed). Interactive Photino verification remains a user-side manual check.
>
> **Baseline:** Desktop 0.2 is merged in [PR #12](https://github.com/Vraxo/Lattice/pull/12), merge commit `ae2cb8d61a2d0e8c81b59259416c15bc2c158193`. Its implementation assignment is preserved at [archive/desktop-0.2.md](archive/desktop-0.2.md).
>
> **User-observed issue:** asking "Show me Program.cs" while connected to a repository reports that `Program.cs` is missing, even though it exists in nested project folders. A user should not have to know the exact relative path when the filename itself is enough to locate candidates.
>
> **Deliverable:** make file requests discover files by basename throughout the connected workspace, handle duplicate filenames explicitly, and replace the current blue-black appearance with a neutral dark-gray/charcoal theme.

## Product intent

Lattice already supports natural-language file requests and source-text search. These are different tasks:

- "Show me Program.cs" asks Lattice to locate and open a file.
- "Where does Program appear?" asks Lattice to search source-text occurrences.

Do not treat a filename request as a source-text search. Do not assume a bare filename is located at the workspace root.

When a user supplies an exact relative path, open that path. When they supply only a filename and it is not at the root, search the connected workspace for matching basenames. If one file matches, open it. If several files match, list their full relative paths and ask which one the user means. Never choose an arbitrary match.

The theme should be dark gray rather than blue-black: neutral charcoal backgrounds and panels, subtle gray borders, readable off-white text, restrained neutral controls, and no dominant blue cast. Keep the existing layout and interaction model unless small adjustments are needed to make the palette coherent.

## Definition of done

### File discovery

- A root-level file requested by basename opens normally.
- A nested file requested only by basename is found without requiring its folder path.
- When the basename matches exactly one file in the workspace, Lattice opens that file inline and shows its relative path.
- When several files share the basename (for example, multiple project-level `Program.cs` files), Lattice lists the matching relative paths and asks the user which one to open.
- The candidate list is useful and complete within a documented, bounded result limit. If more matches exist than can be displayed, Lattice says how many were omitted.
- Lattice does not read a file until the selected relative path is unambiguous.
- The user can respond with a displayed relative path (for example, `Lattice.Desktop/Program.cs`) and have that exact file opened.
- An unmatched filename produces a friendly "couldn't find a file with that name" response rather than a misleading root-only missing-file error.
- An explicit relative path continues to take precedence and is never replaced by a same-basename match elsewhere.
- File discovery stays within the connected workspace, applies existing symlink/reparse-point protections, and follows appropriate traversal exclusions/limits. It must not follow linked directories or expose outside-root paths.
- Existing source-text search, file reading, recent-file follow-ups, and read-only policy remain intact.

### Theme

- Main background and surfaces use neutral dark gray/charcoal values, not blue-black/navy.
- User/assistant messages, cards, evidence panels, file viewers, composer, workspace header, buttons, borders, and focus states all belong to the same cohesive neutral palette.
- Maintain sufficient contrast for body text, muted text, path labels, and keyboard focus.
- Avoid strong blue buttons, blue-tinted panels, or saturated gradients. Use restrained neutral gray accents; preserve clear error/connection status communication.
- The layout, readability, and existing chat interaction are not regressed.

### Verification

- Automated tests cover root and nested basenames, one match, multiple matches, no match, exact relative-path precedence, explicit selection, traversal/link safety, and existing chat/source-search behavior.
- UI tests verify candidate paths and clarification are displayed in the conversation and that no file is read while multiple candidates remain.
- CI restore/build/test passes on Windows with .NET 10.
- README and Windows smoke-test instructions document basename discovery and the new palette.
- Native Photino GUI launch remains a manual user check; CI and bUnit must not be represented as a manual launch.

## Working rules

1. Read this file, `AGENTS.md`, `README.md`, and `docs/DESKTOP_DIRECTION.md` before editing.
2. You are authorized to complete the full Desktop 0.3 assignment without asking for approval between phases.
3. Inspect Git status and main before editing. Create one feature branch from the current main.
4. Work through the phases in order; keep the app runnable and preserve the current chat workflow.
5. Reuse the existing workspace access boundary and traversal rules. Do not add an unsafe recursive filesystem walk in the UI layer.
6. Keep filename discovery distinct from literal source-text search. Use a small, deterministic filename lookup; do not introduce fuzzy matching.
7. Prefer exact, case-insensitive basename matching for bare filename requests. If platform filesystem semantics require a different comparison, document and test it.
8. For several matches, return a bounded candidate list with full relative paths and require explicit selection. Never choose the first match silently.
9. Preserve read-only permissions and all existing workspace confinement, link/reparse-point protections, size/encoding checks, and untrusted-content rules.
10. You may commit and push logical checkpoints to one feature branch and open one draft PR targeting main. Never commit to main, force-push, or merge.
11. Use Windows/.NET 10 CI for restore/build/test if the local SDK is unavailable. Fix CI failures before continuing; do not weaken tests.
12. Do not claim a native Photino window was launched in the headless agent environment. Leave the manual launch check pending.
13. Do not pause for intermediate user reports. Complete the scope, keep the checklist accurate, and return one final report with the PR and exact CI evidence.

---

# Phase 0 — Inspect the merged Desktop 0.2 implementation

- [x] Inspect the current main commit, working tree, solution, and CI workflow.
- [x] Trace file requests through the pattern catalog, `DesktopRequestProcessor`, `DesktopSession`, and existing read/file-discovery boundaries.
- [x] Inspect `WorkspaceRoot`, `WorkspaceFileAccess`, `FileSearchTool`, and `ListDirectoryTool` before choosing the safe place for basename lookup.
- [x] Identify the current traversal exclusions, result limits, and symlink/reparse-point handling. Reuse them where possible.
- [x] Inspect all existing Desktop tests for missing files, recent-file contexts, multiple candidates, workspace switching, and UI rendering.
- [x] Audit `app.css` for the existing blue/navy color values and identify every affected component/state.

**Done when:** The implementation plan uses the existing safe workspace boundary and the theme change is limited to presentation.

> **Phase 0 note:** Inspected `main` at `7d1b5a0af06683e9683ff3c7326f9c0cd51b372e` (green Windows/.NET 10 CI run `42450268128`, following Desktop 0.2 merge `ae2cb8d61a2d0e8c81b59259416c15bc2c158193` with 789 Core + 71 Desktop = 860 passing tests). `WorkspaceFileAccess` is `internal` to `Lattice.Core`, so workspace-wide basename discovery belongs in `Lattice.Core` (`WorkspaceFileLocator`) reusing `WorkspaceFileAccess.ResolveReadableDirectory`, `WorkspaceFileAccess.EnumerateDirectoryEntriesSafely`, and the shared `[".git", ".vs", "artifacts", "bin", "obj"]` exclusion set without reading file contents. Audited all blue-black/navy styles across `Lattice.Desktop/wwwroot/app.css`.

# Phase 1 — Find files by basename

### 1.1 Exact path first, basename fallback second

- [x] Preserve the current behavior for an exact relative path.
- [x] When a request contains only a basename and the exact root-relative file does not exist, search nested directories under the connected root.
- [x] Match the filename itself, not file contents, and do not interpret this request as a source-symbol search.
- [x] Reuse existing safe traversal/exclusion logic or introduce the smallest shared filename lookup at the correct boundary.
- [x] Ensure the lookup cannot follow symlink/reparse-point files or directories, cannot escape the root, and has an explicit result bound.
- [x] Keep the operation read-only and avoid reading candidate file contents merely to enumerate filenames.

### 1.2 Handle zero, one, and multiple matches

- [x] One match: route its relative path through the existing `ReadFileTool` path and display the actual contents.
- [x] Multiple matches: display a numbered or clearly separated list of full relative paths and ask which one to open.
- [x] Do not choose a candidate automatically when more than one file matches.
- [x] Preserve candidate relative paths in the narrow recent-file/conversation context only as needed for a user to select one.
- [x] Accept a subsequent explicit relative path from the candidate list and open that exact candidate.
- [x] Zero matches: explain that no file with that name was found anywhere in the connected workspace.
- [x] For very large result sets, show a clear truncated list and the total count; do not flood the chat.

**Done when:** "Show me Program.cs" locates files in nested projects, opens the file if unique, and asks for an explicit path when duplicates exist.

> **Phase 1 note:** Verified in Windows/.NET 10 CI run `42450939400` on Draft PR #13 (`Lattice.Core.Tests`: 807/807, `Lattice.Desktop.Tests`: 82/82, 889 total passed, 0 failed, 0 skipped, 0 new warnings).

# Phase 2 — Apply the neutral charcoal theme

- [x] Introduce a small set of CSS custom properties for the neutral palette rather than leaving many unrelated hard-coded shades.
- [x] Use a charcoal main background (approximately `#222222`–`#252525`), slightly lighter panels (approximately `#2A2A2A`–`#303030`), darker inset surfaces, and subtle gray borders.
- [x] Replace saturated blue message bubbles, buttons, focus rings, and blue-tinted panels with restrained neutral gray treatments.
- [x] Keep readable off-white primary text and legible muted gray secondary text.
- [x] Preserve visual distinction between user and assistant turns through contrast, alignment, and spacing rather than strong color.
- [x] Keep success/error/clarification states recognizable without turning the overall theme colorful.
- [x] Verify code blocks, file paths, evidence snippets, composer, workspace badge, hover/disabled states, and focus indicators in the new palette.
- [x] Respect `prefers-reduced-motion` and do not introduce visual regressions at narrow widths.

**Done when:** The full application has a cohesive neutral dark-gray appearance with good text/focus contrast, and no dominant blue-black styling remains.

# Phase 3 — Regression tests, docs, and CI

- [x] Add unit tests for root basename, nested unique basename, duplicate basename in multiple projects, missing basename, and exact-path precedence.
- [x] Add tests for case variation, filenames with dots/spaces where supported, and ordinary final punctuation.
- [x] Add safety tests proving filename lookup does not follow linked files/directories, escape the workspace, or enumerate excluded/generated directories contrary to the shared traversal policy.
- [x] Add session/UI tests for candidate listing, explicit selection, no-match wording, and no read before a candidate is selected.
- [x] Run the existing Desktop and Core test suites; preserve all Desktop 0.2 functionality.
- [x] Update README and `docs/windows-smoke-test.md` with "Show me Program.cs" behavior and duplicate-path clarification.
- [x] Inspect the final diff for unsafe traversal, unintended capabilities, inconsistent colors, and accidental changes to source-text search.
- [x] Run full Release restore/build/test in Windows/.NET 10 PR CI and record exact results.
- [ ] Keep the native Photino GUI smoke check explicitly pending for the user.

**Done when:** The file-discovery behavior, theme, and regressions are covered by automated tests and green CI, with only manual window verification remaining.

> **Phase 3 verification log:**
> - Pull Request: `https://github.com/Vraxo/Lattice/pull/13` (`arena/1f7e469e-lattice` -> `main`)
> - Windows `.NET 10` Release CI run `42451266301` (commit `fb9090c`):
>   - `Lattice.Core.Tests`: **807 passed**, 0 failed, 0 skipped
>   - `Lattice.Desktop.Tests`: **84 passed**, 0 failed, 0 skipped
>   - Total: **891 passed**, 0 failed, 0 skipped, 0 new warnings (only the 10 pre-existing StyleCop `SA1515`/`SA1516` warnings in untouched `Lattice/*.cs`)
> - Manual Windows Photino GUI launch (`docs/windows-smoke-test.md`) remains unchecked for interactive Windows verification.

---

## STOP — one draft PR for review

After Phases 0–3:

1. Push the feature branch and open one draft PR targeting main.
2. Keep all checkpoints on the same PR and fix CI failures.
3. Mark the PR ready for review when automated verification passes, but do not merge it.
4. Report the PR link, how basename lookup handles zero/one/multiple matches, palette summary, exact per-project test results, and remaining manual Windows checks.
5. Stop. Do not start another milestone.
