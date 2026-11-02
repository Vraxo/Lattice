# TODO — Lattice Work Queue

**Checkboxes are work items.** Take one, do it, tick it, report, stop. Nothing else in this file is
a checkbox — the bullets are rules you obey, not things you "finish".

**Verified state when written:** build green · **714 tests passing**, 0 failed, 0 skipped ·
**244 build warnings, untriaged**.

---

## Read before starting (reference — not tasks)

- `docs/LATTICE_HANDOFF.md` — operating contract, full file.
- `docs/AI_WORK_PROTOCOL.md` — especially §8 (testing practice).
- `docs/ARCHITECTURE.md`
- `docs/ROADMAP.md` — authoritative for gates.
- `docs/DECISIONS.md` — ADR‑001…009.
- `docs/M8_DESIGN.md`, `docs/RETRIEVAL_BENCHMARK.md`
- Then: attempt `dotnet build` and `dotnet test`. If commands are unavailable, say so plainly and
  ask the user to run them and paste output. **Never invent a test count.** Report the real
  baseline, reconcile the count against the files (`[Theory]` expands **per data row**), then state
  the next task's acceptance gate back to the user and wait.

---

## Rules (reference — not tasks)

**Process**
- One task per request. Stop at its gate.
- **ASK** before any decision affecting committed interfaces, contradicting a document, or expensive
  to reverse. One direct question, not a sweeping assumption.
- **Never commit.** Propose a commit message; the user commits.
- Never claim a command ran unless it ran. Admit uncertainty and your own mistakes.

**Engineering**
- No tool-name conditionals in Core; capability selection via descriptor metadata only.
- Permission + cancellation enforced in `ToolExecutor`, before the tool runs (ADR‑007).
- `AgentLoop` must not branch on incoming statement type.
- File/encoding/confinement/backup logic lives in `WorkspaceFileAccess` only.
- Error codes are string constants in static classes (`FileToolErrorCodes`, `DocumentErrorCodes`,
  `ToolErrorCodes`, `CommandErrorCodes`, `EditErrorCodes`, `ResponseErrorCodes`).
- Spaces not tabs. One class per file. Nullable + implicit usings on.
- Tests deterministic and offline — no network, no uncontrolled clock.

**Traps — every one of these has actually bitten**
- `ImmutableArray<T>` and `ImmutableDictionary<K,V>` compare by **reference**. Use `SequenceEqual`;
  on records containing them, implement explicit content-based `Equals`/`GetHashCode`.
  Pin with **populated, differently-ordered** collections — an empty-collection test passes by
  accident.
- Bare collection expression `[0x00, …]` infers `int[]`; `StartsWith` then binds the wrong overload.
  Use `private static readonly byte[]` fields.
- `Enumerable.Any(...)` needs a `bool` predicate — not a helper returning `double`.
- Read a file before editing. If an exact-match edit fails, **do not retry** — read and rewrite
  (suspect CRLF).
- If `create_file` silently fails on a path: 64-char stub, then `rewrite_file`.
- Never hand-write tool-call closing tags inside generated content.
- `grep(pattern, path)` needs a **directory**; `read_file` for single files.
- Verify every write (`exists` / `read_file`). A "success" message is not proof.
- Derive expected values from the algorithm **before** asserting them.

**Your completion report must address each of these**
1. Acceptance gate met, with evidence.
2. Only the files needed for this task changed — no bundled refactors, no future milestones.
3. Every write verified.
4. Build + tests run (by you, or by the user with output pasted) — real output only.
5. Test count reconciled arithmetically against the files.
6. Behavior: what works, and what deliberately does not.
7. Risks / deviations.
8. Gate PASS/FAIL.
9. Suggested commit message.
10. Next task only — then stop.

---

## Work queue

### M9.3 — Evaluate optional frozen embeddings (retrieval provider only)

**Gate:** document measured benefit, operational cost, licensing, and opt-out behavior before adoption.
If benefit is unclear, do not implement; record the evaluation and stop.
**Status (2026-11-02):** evaluation complete; quantitative gate passed. Recommendation is a separate
integration proposal only. No production provider or dependency was approved or added.

- [x] **ASK** which query class should improve beyond lexical retrieval and what evidence threshold
      justifies a proposal: documentation paraphrases with low lexical overlap; ≥ +10 pp Recall@5,
      ≥ +0.05 MRR@5, no exact-match or overall regression, plus acceptable qualitative gates.
- [x] Inspect M9.1/M9.2 benchmark and identify the measured gap: lexical documentation search still
      misses independently classified low-overlap paraphrases.
- [x] Gap branch selected: measured gains clear the thresholds; no “insufficient benefit” stop.
- [x] **ASK** before evaluation dependencies/model: approved for isolated evaluation only, with
      licensing, source/provenance, maintenance cost, and optionality reviewed first.
- [x] Keep the experiment evaluation-only: frozen model, no training/fine-tuning/generative use, no
      mandatory production dependency, no provider or graph traversal.
- [x] Verify results: low-overlap Recall@5 improves by +28.57 pp and MRR@5 by +0.4063; existing
      exact/alias controls remain 1.0 and overall metrics do not regress. Lexical remains the only
      production path. A provider-specific opt-out switch is not applicable here; any separate
      provider proposal must implement and test explicit disable/fallback.
- [x] Record before/after metrics, operational measurements, licensing, provenance limits, and
      opt-out status in `docs/RETRIEVAL_BENCHMARK.md` §9; update the roadmap outcome note.
- [x] CI build/test and M9.3 evaluation passed. Existing suite count: 714 passed, 0 failed, 0 skipped
      (612 + 46 + 29 + 14 + 3 + 4 + 6 = 714); evaluation harness is separate from normal tests.
      Report and stop; do not begin integration or M9.4 without a separate request.
---

### M9.4 — Add one non-C# package/tool adapter

**Gate:** the new capability can be installed/disabled without changes to core orchestration beyond
its generic contracts.
**Status (2026-11-02):** complete. The optional Python syntax adapter is outside Core; submitted
source is inspected as Python AST syntax and is never executed. PR #6 merged at `9071b19`; CI
build/test passed.

- [x] **ASK** which ecosystem/capability (roadmap says "non-C#"; the concrete choice needs a ruling).
- [x] Inspect `CapabilityDiscovery`, `ToolDescriptor`, `ToolRegistry`, `ToolExecutor`, and how
      `Lattice.CSharp` registers itself.
- [x] Implement the adapter outside Core; Core gains no reference to it.
- [x] Supply descriptor metadata (id, aliases, tags, description) so it is discoverable from the
      descriptor alone.
- [x] Test discovery and invocation through the generic boundary.
- [x] Test clean disable with no change to Core orchestration.
- [x] Test the permission gate still applies — no bypass via the new adapter.
- [x] Run build + tests; reconcile count; report; stop. CI passed; exact test count was not
      available in the CI summary retained for this queue.

---

### M10.1 — Lock a regression request set

**Gate:** fixtures version-controlled with explanations for expected results.
**Status (2026-11-02):** complete. PR #7 CI build and tests passed. The regression theory has nine
rows. Based on the last recorded 714-test baseline, nine M9.4 test cases, and these nine rows, the
expected total is 732; the CI status summary did not expose an observed test count, so 732 is an
arithmetic estimate, not a reported test-run total.

- [x] Inspect existing corpora (`controlled`, `workspace`, `responses`, `retrieval`) and the
      `tests/corpus` copy rules in `Lattice.Core.Tests.csproj`; JSON fixtures preserve nested
      directories under `corpus/` in test output.
- [x] Build versioned fixtures covering: understanding, clarification, tool selection, state
      updates, tool errors, permissions, file preservation, and output grounding.
- [x] Give each entry an explicit rationale for its expected result.
- [x] Assert the fixtures in tests; document that expected answers are not changed casually.
- [x] Run build + tests; CI passed. Nine theory rows add nine test cases; arithmetic estimate is
      732 total (714 + 9 M9.4 tests + 9 M10.1 rows), not an observed CI test count. Report and stop.

---

### M10.2 — Add loop/resource safeguards

**Gate:** tests prove all limits stop the agent safely.

- [ ] Inspect `AgentLoop`, `ToolExecutionLimits`, cancellation plumbing, existing repeated-action
      handling.
- [ ] Enforce: maximum steps; repeated-action detection; per-tool timeouts; cancellation; observable
      budget exhaustion.
- [ ] Each limit yields a distinct typed terminal outcome — not a crash.
- [ ] Test each limit stops safely and observably.
- [ ] Timing-dependent tests: run repeatedly — one green run is not evidence.
- [ ] Run build + tests; reconcile count; report; stop.

---

### M10.3 — Adversarial tool-output tests

**Gate:** untrusted content cannot bypass executor permissions.

- [ ] Inspect `ToolExecutor`, permission policy, and how tool output reaches state.
- [ ] Fixtures: file/web/tool text attempting to override policy or trigger unauthorized actions.
- [ ] Test permissions are not bypassed; untrusted text is never executed as control.
- [ ] No production change unless a real defect is found — if found, **ASK** before altering
      security behavior.
- [ ] Run build + tests; reconcile count; report; stop.

---

### M10.4 — Capability matrix and known limitations

**Gate:** each advertised capability points to test coverage or a clearly marked manual limitation.

- [ ] Enumerate capabilities from the tool registry and roadmap.
- [ ] Classify each: supported / partial / unsupported, with a pointer to tests or a marked
      limitation.
- [ ] State the Markdown subset honestly: ATX headings (≤3 leading spaces, optional closing hashes),
      fenced code (3+ backticks/tildes). **Setext headings, tab indentation, inline formatting, and
      list nesting are NOT supported.**
- [ ] State encoding honestly: reads permissive; edits refuse anything but UTF‑8-no-BOM.
- [ ] No overclaiming — every line backed by a test or explicitly marked as a limitation.
- [ ] Run build + tests; report; stop.

---

## Blocked — raise when relevant, do not act

- **244 untriaged build warnings** (up from 47 earlier). Unknown how many are new. Ask whether a
  warning-triage task should be inserted before M9.3.
- **`ClassMetricsMatchRecordedBaseline`** asserts pre-M9.2 behavior via the old exact adapter —
  intentional historical record, but now asserts superseded behavior. Ask whether to keep, retire,
  or move the baseline to docs only.
- **`related-concept` graph traversal** — out of scope for M9.2; needs a separately scoped
  follow-up. Ask whether the benchmark justifies it.
- **Structured tool diagnostics** — `ToolResult` carries one scalar `ArgumentValue`, so diagnostics
  flatten to text. A **contract** question, not transport. Ask before designing.
- **`AgentLoop` / CLI wiring to M8 tools** — not wired. Ask whether this is in a milestone or
  deferred.
- **Encoding-preserving writes** — future work; must arrive with regression tests before replacing
  the current refusal. Ask before starting.

---

## Debt — do not fix unprompted

- `MarkdownEditTool.cs` — unused `using System.Globalization;`
- `TextEditor.CharacterOffset` is O(n) → quadratic on repeated large-document edits.
- No `markdown.read`; callers must `files.read`, parse, and hash the section for `expectedHash`.
- `RetrievalBenchmarkTests` uses private nested records for corpus rows (test-only; the protocol
  discourages private nested types generally).

---

## Appendix — reference

**Commands:** `dotnet build` · `dotnet test` (repo root `E:\Dev\Lattice`, .NET 10 preview, `net10.0`)

**Test count checkpoints — reconcile against these:**

| Point | Total |
|---|---|
| Session baseline | 624 |
| + M8.4 encoding | 650 |
| + M8.3 templates | 677 |
| + M8.1a edit model + list test | 691 |
| + M9.1 benchmark | 700 |
| + M9.2 lexical retrieval | **714** |

**Layers:** `Lattice/` Core (no compiler/UI/FS deps) · `Lattice.CSharp/` Roslyn adapter ·
`Lattice.Cli/` composition root · `Lattice.Core.Tests/` xunit.

**Corpora:** `tests/corpus/{controlled,workspace,responses,retrieval}/v1.json` — auto-copied to
output; verify with a run.

**Commit style:** `type(scope): imperative summary` — e.g.
`feat(retrieval): add measured lexical retrieval improvements`.