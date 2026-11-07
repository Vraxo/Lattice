# Lattice — Desktop 0.2: Conversational Workspace Interaction

> **Status:** in progress on `arena/9b7807a5-lattice` (starting from `main` at `477e732c7f07155445b4ca22419b3dd92733ac1b`).
>
> **Previous checkpoint:** Desktop 0.1 is merged into main at `77a857bde251b21083a02db75501bac1d44fee43`. Its completed checklist is preserved at [archive/desktop-0.1.md](archive/desktop-0.1.md).
>
> **Deliverable:** turn the existing read-only workspace console into a genuinely conversational desktop experience. Users should ask ordinary, bounded questions about their connected codebase; they should not need to memorize tool commands.

## Project intent

Desktop 0.1 proved that Lattice can connect to a workspace, inspect files, search source text, and show evidence. Its interface exposed those operations as a command form with a Run button. That is not the intended product interaction.

Desktop 0.2 changes the primary interaction model:

- The user writes messages in a multiline chat composer.
- User messages and Lattice replies appear as distinct conversational turns.
- Lattice recognizes a bounded set of natural phrasings and selects the existing read-only capabilities.
- File contents and search evidence appear as part of the assistant's response.
- A narrow follow-up reference works only when the preceding conversation provides one unambiguous target.
- When Lattice cannot confidently interpret a request, it asks a useful clarification or explains its current limits in ordinary language.

This is **bounded symbolic language understanding**, not unrestricted English comprehension and not an LLM. Do not overstate its abilities. A natural sentence is accepted because an explicit deterministic pattern supports it, not because the implementation guesses.

## Definition of done

Desktop 0.2 is complete when:

- The primary interface behaves and looks like a chat application, not a command console.
- A multiline composer supports sending with Enter and inserting a newline with Shift+Enter.
- User messages and assistant responses are visually distinct and remain in chronological history.
- The workspace connection remains available but no longer dominates the main interaction area.
- The UI does not teach or require `inspect <path>` or `find symbol <name>` syntax.
- These natural phrasings work against a connected workspace:

  **Finding source occurrences**
  - "Where does WorkspaceRoot appear?"
  - "Where is WorkspaceRoot used in this project?"
  - "Can you find where WorkspaceRoot is used throughout this project?"
  - "Search this project for WorkspaceRoot."
  - "Find all occurrences of WorkspaceRoot."

  **Viewing a file**
  - "Show me Lattice/WorkspaceRoot.cs."
  - "Open the file Lattice/WorkspaceRoot.cs."
  - "Can you read Lattice/WorkspaceRoot.cs?"
  - "What's in Lattice/WorkspaceRoot.cs?"

- Search results retain real relative paths, line numbers, and snippets; inspected files show their actual contents.
- Assistant wording is conversational but grounded in returned tool results. It does not invent conclusions about code semantics.
- The literal source-text search limitation remains clear: occurrences are not compiler-resolved declarations or references.
- A follow-up such as "Show me that file" resolves only when conversation history identifies exactly one file. If several files are plausible, Lattice asks which one rather than guessing.
- Unsupported and ambiguous input receives a helpful natural-language response, not a syntax error or a list of internal command forms.
- Workspace confinement, symlink/reparse-point protection, read-only policy, file-size limits, and untrusted-file-content handling remain intact.
- Automated tests cover phrase interpretation, request routing, follow-up context, and rendered chat behavior.
- Windows/.NET 10 CI passes the full solution and test suite.
- README and smoke-test instructions describe the new interaction accurately.
- One draft PR targeting main is ready for review, unmerged, with manual Windows checks clearly identified.

## Boundaries

- No LLM, model, embeddings, training, remote inference, or network-backed language service.
- No new tool permissions, file writes, edit proposals, shell execution, or build-command execution.
- No weakening or bypassing workspace safety guarantees.
- No semantic-symbol-resolution claims. Search remains literal, case-insensitive source-text search.
- No open-ended agent/planner architecture. This increment is about a more natural interface and better bounded request interpretation.
- Preserve the existing Core, Desktop, tests, and CI structure unless a narrowly justified change is needed for phrase matching.

## Working rules

1. Read this file fully and work through its phases in order.
2. You are authorized to complete the entire Desktop 0.2 scope without stopping for approval between phases.
3. Keep the application runnable throughout. A static UI mock-up disconnected from workspace operations is not a deliverable.
4. Reuse existing workspace tools and evidence flows. Do not build duplicate search/read implementations.
5. Make the smallest deterministic extension to the existing pattern interpreter that supports the required phrases. Preserve existing behavior and ambiguity reporting.
6. A pattern may need fixed words before and after its captured value (for example, `where does {symbol} appear`). If extending the grammar, keep it small: at most one capture slot, bounded literal prefix/suffix tokens, deterministic matching, and explicit ambiguity on ties. Do not create a general NLP framework.
7. Preserve punctuation and identifiers/paths correctly. Ignore ordinary sentence-final punctuation when matching without corrupting dots, slashes, underscores, hyphens, or file extensions inside captured values.
8. Resolve "that file"/"it" only from a single unambiguous recent file reference; otherwise ask which item.
9. Inspect Git status before editing and preserve user changes.
10. You are authorized to commit and push small logical checkpoints to one feature branch and open one draft PR targeting main. Never commit directly to main, force-push, or merge the PR.
11. The repository PR CI uses Windows and .NET 10. Push checkpoints to the same PR so CI verifies each update. Fix failures before proceeding; do not weaken tests to get a green run.
12. Do not block on the missing local .NET SDK. Use Windows CI for restore/build/test verification and report exact logs/counts.
13. The agent environment is headless. Complete automated UI tests and implementation work, but do not claim a native Photino window was manually launched. Leave that check for the user.
14. Update checkboxes only when acceptance criteria are met. Never mark a manual GUI action complete based on bUnit or CI.
15. After the plan is implemented and CI passes, mark the PR ready for review (not merged) and stop. Provide one final report with PR link and remaining manual Windows checks.

---

# Phase 0 — Confirm the Desktop 0.1 baseline

### 0.1 Inspect the current application

- [x] Inspect current main, working tree, solution, Desktop project, Desktop tests, and CI workflow.
- [x] Read the archived Desktop 0.1 checklist and current Windows smoke test to identify behavior that must remain intact.
- [x] Trace the UI flow through `App.razor`, `DesktopSession`, and `DesktopRequestProcessor`.
- [x] Trace pattern parsing/matching through `IntentPattern`, `IntentPatternCatalogParser`, `RequestInterpreter`, and `config/nlp/patterns.json`.
- [x] Identify existing tests for session history, request matching, search evidence, inspection, ambiguity, and unsupported input.

**Done when:** The agent knows which UI and interpreter components must change and which read-only behavior must remain untouched.

### 0.2 Record the known baseline

- [x] Preserve the Desktop 0.1 CI baseline: Windows/.NET 10, 749 Core tests + 38 Desktop tests = 787 passed, 0 failed, 0 skipped (PR #11 CI run 42394747121).
- [x] Confirm the merged main commit and current PR CI workflow before creating the feature branch.
- [x] Do not spend time attempting local dotnet commands if the SDK is unavailable; use PR CI for code checkpoints.

**Done when:** The starting commit and known baseline are recorded accurately. Distinguish pre-existing warnings/failures from regressions.

> **Phase 0 note:** Confirmed `main` at `477e732c7f07155445b4ca22419b3dd92733ac1b` (following Desktop 0.1 merge `77a857bde251b21083a02db75501bac1d44fee43`) with green Windows/.NET 10 CI run `42397651659` (`Lattice.Core.Tests`: 749/749, `Lattice.Desktop.Tests`: 38/38, 787 total passed, 0 failed, 0 skipped; 10 pre-existing StyleCop `SA1515`/`SA1516` warnings in untouched `Lattice/*.cs`; 0 warnings in `Lattice.Desktop` / `Lattice.Desktop.Tests`). Local `.NET` SDK is absent in this container, so checkpoints are verified via PR CI on `arena/9b7807a5-lattice`.

---

# Phase 1 — Replace the request console with a chat interface

### 1.1 Build the conversation layout

- [x] Replace the "Workspace Request" panel and "Run" command form with a conventional chat layout.
- [x] Give user messages and assistant replies distinct alignment, styling, and accessible semantics.
- [x] Keep history chronological and preserve inline file viewers/evidence cards inside assistant turns.
- [x] Keep the workspace indicator visible but compact; the conversation should dominate the window.
- [x] Add a useful empty state inviting the user to ask about the connected codebase.
- [x] Remove visible instructions that teach `inspect <path>` or `find symbol <name>` as required syntax.

### 1.2 Implement the chat composer

- [x] Replace the single-line input with a multiline textarea and a clearly labeled Send action.
- [x] Enter submits; Shift+Enter inserts a newline. Ensure reliable, accessible behavior.
- [x] Keep sending disabled while a request is processing; show a subtle in-conversation processing state, not "Running command".
- [x] Clear the composer only after a message is accepted for processing; do not lose text on validation/unexpected failure.
- [x] Keep focus behavior and scrolling usable as turns are appended.
- [x] Add bUnit tests for composer rendering, input, Enter/Shift+Enter, Send, busy state, and history display.

**Done when:** A user sends a conversational message, sees it as a user bubble, and receives the existing real workspace operation's result as an assistant turn. The interface no longer resembles a command console.

> **Phase 1 note:** Verified in Windows/.NET 10 CI run `42404067427` on PR #12 (`Lattice.Core.Tests`: 749/749, `Lattice.Desktop.Tests`: 44/44, 0 failed, 0 skipped, 0 new warnings).

---

# Phase 2 — Extend bounded language interpretation

### 2.1 Natural phrasings for source search

- [x] Add deterministic patterns for all search phrasings in the Definition of done.
- [x] Support a single captured identifier surrounded by known literal words where needed, including fixed trailing words such as "appear", "used in this project", and "used throughout this project".
- [x] Prefer the smallest compatible extension to the existing pattern representation/interpreter; no fuzzy matcher, embeddings, ML, or unrestricted regex routing.
- [x] Match case-insensitively and tolerate ordinary terminal punctuation while preserving the captured identifier.
- [x] Keep competing interpretations explicit and deterministic; never silently choose an arbitrary action.
- [x] Preserve explicit forms like `find symbol WorkspaceRoot` as optional compatibility aliases, but do not present them as the primary interaction.

### 2.2 Natural phrasings for file inspection

- [x] Add deterministic patterns for "show me", "open the file", "read", and "what's in" phrasings.
- [x] Preserve path characters, extensions, separators, and valid spaces; reject ambiguity rather than guessing.
- [x] Keep reads routed through the existing `ReadFileTool` and workspace access boundary.
- [x] Preserve `inspect <relative-path>` as an optional compatibility alias.

### 2.3 Verify the phrase grammar

- [x] Add unit tests for every required phrase, casing, sentence punctuation, identifiers with underscores/dots, and file paths with extensions.
- [x] Add negative tests for unsupported phrasing, empty/missing captures, ambiguous matches, and malformed templates.
- [x] Verify existing pattern catalogs remain compatible, or document/test a deliberate schema-version change.
- [x] Preserve original user text and expose the matched pattern/capture for diagnostics.

**Done when:** All required phrases map deterministically to existing bounded intents, while ambiguity and unsupported input remain explicit.

> **Phase 2 note:** Verified in Windows/.NET 10 CI run `42404988743` on PR #12 (`Lattice.Core.Tests`: 789/789, `Lattice.Desktop.Tests`: 58/58, 0 failed, 0 skipped, 0 new warnings).

---

# Phase 3 — Conversational, evidence-grounded replies

### 3.1 Search replies

- [x] Route natural-language search requests through the existing `WorkspaceQuestionAnswerer` and `FileSearchTool`.
- [x] Produce concise natural replies based on actual counts/evidence, e.g. "I found 8 matching occurrences across 3 files."
- [x] Keep each citation's relative path, 1-based line number, and matching snippet.
- [x] Explain that results are literal source-text matches, not semantic declarations/references, without repeating a large warning after every result.
- [x] Preserve clear no-match and access-error responses in conversational wording.

### 3.2 File replies

- [x] Route natural-language file requests through the existing safe read path.
- [x] Present the file as an assistant response with filename, line count, and readable inline file viewer.
- [x] Handle missing paths, directories, invalid paths, oversized files, invalid encoding, and symlink/reparse-point rejection in plain language.
- [x] Never show raw internal error codes or require users to memorize command syntax.

### 3.3 Unknown and ambiguous messages

- [x] Replace "Unsupported request" and "Supported forms are ..." with brief, useful explanations of what Lattice can currently help with.
- [x] For genuinely ambiguous input, ask one specific clarification instead of returning raw pattern IDs or diagnostics.
- [x] Do not claim a file was read or searched unless the corresponding tool ran and returned evidence.
- [x] Keep the original user message visible in the transcript.

**Done when:** Users see ordinary assistant replies grounded in actual results, rather than tool-oriented status strings.

> **Phase 3 note:** Verified in Windows/.NET 10 CI run `42405289974` on PR #12 (`Lattice.Core.Tests`: 789/789, `Lattice.Desktop.Tests`: 61/61, 0 failed, 0 skipped, 0 new warnings).

---

# Phase 4 — Narrow conversational follow-up context

### 4.1 Resolve unambiguous recent file references

- [ ] Track only the minimal context needed to resolve a recent file reference; do not add a general memory/knowledge system.
- [ ] Support "Show me that file" or "Open it" only when the preceding result identifies exactly one unambiguous file.
- [ ] If the preceding search result contains several files, ask which file the user means. Do not silently choose the first result.
- [ ] Keep resolved paths relative to the active workspace and route them through the same safe file-read path.
- [ ] Clear/invalidate stale references when the workspace changes.

### 4.2 Test context and ambiguity

- [ ] Add tests for one-file follow-up, multiple-file ambiguity, no preceding reference, workspace switching, and stale/missing targets.
- [ ] Add rendered tests proving clarification appears as an assistant response and no unintended read occurs.

**Done when:** A small, test-covered follow-up works without guessing or weakening workspace confinement.

---

# Phase 5 — UX, regression coverage, and documentation

### 5.1 Polish and regression-test

- [ ] Ensure message hierarchy, spacing, long code blocks, evidence lists, and scrolling work at normal/narrow window sizes.
- [ ] Preserve responsive layout, keyboard access, focus indicators, and accessible labels/roles.
- [ ] Verify the UI does not expose internal pattern/tool IDs, command syntax, or raw exceptions as normal assistant language.
- [ ] Add/complete session and bUnit tests for all Definition of done examples, follow-up context, unknown input, ambiguity, errors, and retained history.
- [ ] Preserve all Desktop 0.1 workspace safety and read-only tests.

### 5.2 CI and documentation

- [ ] Run full Release restore/build/test on Windows/.NET 10 through the draft PR.
- [ ] Verify final CI with exact per-project test counts and zero failures/skips.
- [ ] Update README with conversational interaction and natural-language examples; remove command-console framing.
- [ ] Update `docs/windows-smoke-test.md` to test conversational phrases and follow-up references.
- [ ] Record exact verification evidence and leave manual Windows launch unchecked.

**Done when:** Desktop 0.2 is implemented and CI-verified, with only the genuinely manual GUI smoke test remaining for the user.

---

## STOP — prepare one draft PR for review

After Phases 0–5:

1. Push the feature branch and ensure its draft PR targets main.
2. Ensure the latest PR CI run is green.
3. Mark the PR ready for review, but **do not merge it**.
4. Report the PR link, conversational UI summary, supported phrases, per-project test counts, remaining manual checks, and exact launch command.
5. Stop; do not start another milestone.

## Explicitly out of scope

- General-purpose English conversation or human-level language-understanding claims.
- LLMs, embeddings, model downloads, training, remote APIs, or network inference.
- Compiler-resolved symbol/reference navigation.
- File edits, patches, writes, shell commands, builds, or autonomous code changes.
- General conversation memory beyond the narrow recent-file follow-up in Phase 4.
- Speculative architecture unrelated to the conversational workflow.
