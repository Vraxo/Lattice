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

**Gate:** documented measured benefit, operational cost, licensing, and opt-out behavior **before**
adoption. If benefit is not clear: do **not** implement — record the evaluation and stop.
**Context:** M9.2 already moved `documentation` recall off zero with lexical matching, so the case
for embeddings is weaker than when the roadmap was written. "Evaluated; insufficient benefit; not
adopted" is a legitimate and expected outcome.

- [ ] **ASK** which query class is expected to improve that lexical retrieval cannot serve, and what
      evidence threshold justifies adoption.
- [ ] Inspect the M9.1/M9.2 benchmark and `docs/RETRIEVAL_BENCHMARK.md`; identify the measured gap.
- [ ] If no gap justifies it: record the evaluation and rationale in `docs/RETRIEVAL_BENCHMARK.md`,
      update the roadmap note, run build/tests, report, stop.
- [ ] If a gap does justify it: **ASK** before adding any dependency (license, source, maintenance
      cost, opt-out design approved first).
- [ ] If approved: retrieval provider only — no fine-tuning, no generative model, no mandatory
      dependency; project must remain fully useful without it.
- [ ] Verify exact/alias recall stays 1.0; targeted class improves measurably; opt-out path tested.
- [ ] Update before/after metrics in `docs/RETRIEVAL_BENCHMARK.md`.
- [ ] Run build + tests; reconcile count; report; stop.

---

### M9.4 — Add one non-C# package/tool adapter

**Gate:** the new capability can be installed/disabled without changes to core orchestration beyond
its generic contracts.

- [ ] **ASK** which ecosystem/capability (roadmap says "non-C#"; the concrete choice needs a ruling).
- [ ] Inspect `CapabilityDiscovery`, `ToolDescriptor`, `ToolRegistry`, `ToolExecutor`, and how
      `Lattice.CSharp` registers itself.
- [ ] Implement the adapter outside Core; Core gains no reference to it.
- [ ] Supply descriptor metadata (id, aliases, tags, description) so it is discoverable from the
      descriptor alone.
- [ ] Test discovery and invocation through the generic boundary.
- [ ] Test clean disable with no change to Core orchestration.
- [ ] Test the permission gate still applies — no bypass via the new adapter.
- [ ] Run build + tests; reconcile count; report; stop.

---

### M10.1 — Lock a regression request set

**Gate:** fixtures version-controlled with explanations for expected results.

- [ ] Inspect existing corpora (`controlled`, `workspace`, `responses`, `retrieval`) and the
      `tests/corpus` copy rules in `Lattice.Core.Tests.csproj`.
- [ ] Build fixtures covering: understanding, clarification, tool selection, state updates, tool
      errors, permissions, file preservation, output grounding.
- [ ] Give each entry an explicit rationale for its expected result.
- [ ] Assert the fixtures in tests; document that expected answers are not changed casually.
- [ ] Run build + tests; reconcile count; report; stop.

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