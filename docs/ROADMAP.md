# Incremental Roadmap — Project Lattice

**Operating rule:** one substep at a time. Each substep must be small enough to review, build, test, and commit independently. Do not begin the next substep until the current one passes its gate.

## How to use this roadmap

- A **milestone** is a coherent capability.
- A **substep** is one small implementation increment, normally one focused commit.
- Every substep ends with a build/test result and a concise report of changed files, design choices, known gaps, and proposed commit message.
- If a substep exposes a design flaw, stop and update the plan/decision record before piling on more code.
- Do not implement future milestones early “for convenience.”

---

## M0 — Repository baseline

**Outcome:** a boring, reproducible, empty foundation that builds and tests.

### M0.1 — Inspect or scaffold the repository
- Inspect the repository, installed .NET SDK, existing conventions, and Git status.
- If non-empty, report the current shape before changing anything; do not overwrite or reorganize existing work.
- If empty, create the smallest solution with `Lattice.Core`, `Lattice.Cli`, and `Lattice.Core.Tests`.
- Add `.editorconfig`, shared build settings, `.gitignore`, and a short build/test section in the README.
- Do not implement agent behavior.

**Gate:** clean build and passing placeholder test using the documented commands; no unreviewed unrelated files changed. Commit: `chore: establish solution baseline`.

### M0.2 — Add contribution and verification commands
- Document exact restore/build/test commands and supported SDK.
- Make sure all projects target the chosen framework consistently.
- Add a CI workflow only if the repository host and desired CI are known; otherwise defer it.

**Gate:** a fresh checkout can follow the instructions successfully. Commit: `docs: document local verification workflow`.

## M1 — Domain kernel and structured state

**Outcome:** the program can store and inspect an explicit task state without trying to understand free-form English yet.

### M1.1 — Add minimal identifiers and result types
- Add only identifiers required for sessions, facts, goals, and actions.
- Define explicit success/failure result representation for expected operational outcomes.
- Do not introduce a generic framework for all possible errors.

**Gate:** unit tests cover success, expected failure, and invalid construction. Commit: `feat(core): add foundational result and identity types`.

### M1.2 — Add facts and provenance
- Represent fact content/value, source kind/reference, and status (observed, user-asserted, retrieved, assumed, derived).
- Keep the payload model modest; defer a universal ontology.

**Gate:** tests prove two same-valued claims with different provenance remain distinguishable. Commit: `feat(core): represent facts and provenance`.

### M1.3 — Add goals and constraints
- Represent a goal, completion condition, constraints, and status.
- Do not add automatic planning yet.

**Gate:** deterministic serialization/debug display plus tests for incomplete/completed/blocked goals. Commit: `feat(core): model goals and constraints`.

### M1.4 — Add session state and explicit transitions
- Session includes message history, facts, goals, observations, and action history at a minimal level.
- Make state changes testable; prevent accidental mutation from unrelated components.

**Gate:** tests assert exact state changes after adding a user assertion and a tool observation. Commit: `feat(core): add session state transitions`.

## M2 — Typed capabilities and tool execution

**Outcome:** a registered tool can be selected, validated, run, and recorded without any language understanding.

### M2.1 — Define tool descriptors and schemas
- Stable tool ID, description, input/output contract, permission/side-effect metadata, and execution limits.
- Do not build dynamic code loading yet.

**Gate:** tests reject duplicate IDs and malformed descriptors. Commit: `feat(tools): define tool contracts`.

### M2.2 — Add tool registry and input validation
- Register fake tools and resolve them by ID.
- Validate inputs before invocation.

**Gate:** unknown tool and invalid arguments fail safely; no tool method is invoked. Commit: `feat(tools): add registry and validation`.

### M2.3 — Execute and record deterministic fake tool
- Add one fake operation and a calculator only if safe expression evaluation can be done without unsafe evaluation; otherwise begin with a fixed echo/fixture tool and add arithmetic separately.
- Record invocation, result, and failure state.

**Gate:** successful result, tool failure, timeout/cancellation path (where applicable), and output provenance are tested. Commit: `feat(tools): execute and record tool results`.

### M2.4 — Add permission gate
- Read-only vs write/side-effect metadata and a central policy check.
- Never rely exclusively on the planner to enforce permissions.

**Gate:** denied actions cannot reach the implementation. Commit: `feat(security): gate tool execution by policy`.

## M3 — External knowledge packages

**Outcome:** concepts and rules can be added as data instead of adding core conditionals.

### M3.1 — Design the smallest package format
- Create one example package with a few concepts, aliases, facts, and one simple rule.
- Choose a human-editable format and schema-version policy.
- Do not build a general package marketplace or migration framework.

**Gate:** format can express the real example without custom code. Commit: `docs(knowledge): define first package schema`.

### M3.2 — Load and validate the example package
- Parse schema, validate identifiers and required fields, and report all actionable errors.
- Treat package content as data; no code execution.

**Gate:** valid package loads; malformed, duplicated, and unknown-reference fixtures fail with readable diagnostics. Commit: `feat(knowledge): load and validate packages`.

### M3.3 — Add basic lookup
- Exact ID and alias lookups; return provenance.
- Defer embeddings, vector databases, and broad fuzzy matching.

**Gate:** package lookup tests cover success, no result, ambiguity, and source tracking. Commit: `feat(knowledge): query concepts and aliases`.

### M3.4 — Add minimal rule evaluation
- Apply one small, explicit rule form to known facts.
- Record which premises/rule produced derived facts; prevent infinite repeated derivation.

**Gate:** derivations are deterministic and provenance links are test-covered. Commit: `feat(reasoning): evaluate basic declarative rules`.

## M4 — Controlled language and intent interpretation

**Outcome:** a user can express a small, explicit goal and have it converted to inspectable structure.

### M4.1 — Define an ACE-inspired mini-language
- Specify a small grammar for statements such as facts, goals, constraints, and explicit actions.
- Include positive/negative examples and ambiguity/error cases.
- Keep the grammar small; do not claim full ACE compatibility.

**Gate:** grammar examples are unambiguous and the spec includes a versioned test corpus. Commit: `docs(nlp): define controlled statement subset`.

### M4.2 — Implement lexer/parser for the subset
- Produce typed syntax/meaning records with source spans.
- Do not add ad-hoc parsing for every fixture without updating the grammar.

**Gate:** all valid/invalid fixtures pass. Commit: `feat(nlp): parse controlled statements`.

### M4.3 — Add bounded natural-language patterns
- Support a tiny set of realistic request patterns (for example ask a question, inspect a path, find a symbol, request an explanation).
- Patterns and vocabulary live in configuration/package data where feasible.
- Keep original text and expose matched pattern/slots.

**Gate:** positive, negative, paraphrase, and ambiguous examples are tested; unknown input is not silently assigned a confident intent. Commit: `feat(nlp): interpret initial user request patterns`.

### M4.4 — Add clarification behavior
- Detect missing required fields and materially competing interpretations.
- Generate a concise clarification question through templates.

**Gate:** tests prove the engine asks on genuinely ambiguous fixtures and proceeds on fully specified ones. Commit: `feat(nlp): ask for missing or ambiguous information`.

## M5 — Controller and bounded action loop

**Outcome:** the system selects tools based on state, handles results, and finishes or asks instead of being a bag of disconnected components.

### M5.1 — Define next-action proposals
- Typed variants for invoke tool, ask user, respond, continue/replan, and finish.
- Do not represent actions as strings that must be reparsed.

**Gate:** invalid action payloads fail validation; exhaustive handling is enforced in code/tests. Commit: `feat(agent): model next-action proposals`.

### M5.2 — Add deterministic policy selector
- Select among a handful of candidate actions using explicit preconditions and transparent priority rules.
- Record concise decision evidence: relevant facts/rules and selected action.

**Gate:** identical state gives identical action in deterministic fixtures; no eligible action leads to clarification/blocked status, not a crash. Commit: `feat(agent): select eligible next actions`.

> **Follow-up recorded (M7.2a — descriptor-based capability discovery):** M5.3 as originally
> written left capability selection unimplemented. The loop executed explicitly named tools but
> Core could not *choose* a capability. That gap was closed after M7.2 by `CapabilityDiscovery`,
> which matches a structured operation against generic descriptor metadata (id, aliases, tags,
> description) and routes candidates through the existing `PolicySelector`. The original M5.3
> acceptance gate was therefore **not** satisfied as written at the time; this note records the
> correction rather than pretending otherwise.
### M5.3 — Implement one-turn tool loop
- Interpret a controlled request, select a tool, execute it, store the result, and form a response.
- Keep loop iteration count bounded.

**Gate:** end-to-end tests include success, unavailable tool, invalid input, tool failure, denied permission, and repeated-action prevention. Commit: `feat(agent): run bounded tool interaction loop`.

### M5.4 — Add CLI session trace
- Display user-visible response plus optional concise diagnostics/action trace.
- Avoid dumping enormous state by default; add a debug mode.

**Gate:** a demo can be followed from input to structured intent to tool call to verified result. Commit: `feat(cli): expose agent demo and debug trace`.

## M6 — First useful local-work agent

**Outcome:** the system can safely explore a workspace and answer constrained questions about it.

### M6.1 — Add workspace listing and file reading
- Constrain access to an explicitly configured workspace root.
- Normalize paths and prevent traversal outside allowed roots.

**Gate:** tests cover valid paths, nonexistent paths, traversal attempts, and encoding/read failures. Commit: `feat(files): add bounded workspace read tools`.

### M6.2 — Add text search
- Search file contents/names within approved roots, with limits and source locations.
- Treat file contents as untrusted data.

**Gate:** deterministic fixtures cover matches, no matches, excluded files, size limits, and source spans. Commit: `feat(files): add workspace search`.

### M6.3 — Answer repository questions with evidence
- The system may report paths/snippets and what is known, but must cite file paths and line/range where possible.
- If evidence is insufficient, it asks or says it could not determine the answer.

**Gate:** an evaluation set measures correct file selection and zero unsupported claims on fixtures. Commit: `feat(agent): answer workspace questions with evidence`.

### M6.4 — Generate and preview a patch
- Use patch/edit operations with a preview/diff; do not write files by default at this stage.

**Gate:** tests prove unchanged regions stay unchanged and patch application rejects stale contexts. Commit: `feat(files): propose and preview text patches`.

### M6.5 — Authorized write and rollback information
- Write only after policy/approval permits it; record a diff and preserve recovery information.

**Gate:** denied write leaves files unchanged; approved write produces an exact expected diff. Commit: `feat(files): apply approved patches safely`.

## M7 — C#-aware coding tools

**Outcome:** use compiler-grade structure rather than custom C# parsing.

### M7.1 — Integrate Roslyn in a separate adapter
- Parse C# source and report syntax diagnostics.
- Keep Roslyn references out of the core domain project.

**Gate:** tests use valid and invalid fixtures, including incomplete code. Commit: `feat(csharp): add Roslyn syntax inspection`.

### M7.2 — Add symbol and semantic inspection
- Resolve declarations/references and inspect semantic diagnostics where a project compilation is available.

**Gate:** fixtures cover symbol lookup, type information, unresolved identifiers, and project context. Commit: `feat(csharp): add semantic code inspection`.

### M7.3 — Propose syntax-aware edits
- Begin with one narrow transformation with stable tests; do not begin with arbitrary refactoring.

**Gate:** output parses and the diff changes only the intended construct. Commit: `feat(csharp): add first syntax-aware transformation`.

### M7.4 — Add build/test execution as a guarded capability
- Execute only preconfigured commands or explicitly approved commands, within timeout and workspace constraints.
- Translate output to structured diagnostics while retaining raw output.

**Gate:** tests cover timeout, nonzero exit code, cancellation, and safe command policy. Commit: `feat(csharp): add guarded verification runner`.

## M8 — Prose and structured-document editing

**Outcome:** useful Markdown editing without assuming every problem is code.

### M8.1 — Model edit instructions and preservation constraints
- Support operations such as rewrite selected section, shorten, change tone, preserve meaning, and do not modify other sections.
- Represent target spans and constraints explicitly.

**Gate:** fixture tests confirm scope restrictions and preservation criteria. Commit: `feat(text): model scoped prose edits`.

### M8.2 — Add deterministic document transformation baseline
- Start with operations that can be performed reliably using explicit rules/templates; retain original text and diff.

**Gate:** tests cover headings, lists, code fences, and unchanged-region preservation. Commit: `feat(markdown): add safe structured document edits`.

### M8.3 — Add first English realization templates
- Produce natural-enough explanations from known facts/tool results with templates and grammar-safe slots.
- Avoid a premature attempt at unrestricted prose generation.

**Gate:** response fixtures verify semantic slots are correct and unverified claims are not introduced. Commit: `feat(nlg): realize evidence-based responses`.

## M9 — Retrieval improvement and package extensibility

**Outcome:** evidence-based retrieval and external capabilities scale without rewriting the core.

### M9.1 — Define a retrieval benchmark
- Build a small human-reviewed set of exact-symbol, alias, related-concept, and documentation queries.

**Gate:** baseline precision/recall recorded before adding sophistication. Commit: `test(retrieval): add retrieval evaluation set`.

### M9.2 — Add configurable lexical/fuzzy retrieval
- Add only if benchmark results demonstrate a gap; maintain source attribution and explainable scoring.

**Gate:** no regression on exact matches and improved performance on the targeted query class. Commit: `feat(retrieval): add measured lexical retrieval improvements`.

### M9.3 — Evaluate optional frozen embeddings
- Benchmark one existing embedding system as a retrieval provider only.
- No fine-tuning, no generative model, and no mandatory dependency.

**Gate:** documented measured benefit, operational cost, licensing, and opt-out behavior before adoption. If the benefit is not clear, do not merge it. Commit: `docs(retrieval): evaluate optional embedding provider` (or stop without implementation).

### M9.4 — Add one non-C# package/tool adapter
- Prove extensibility with a second capability or language rather than a generic plugin system built only in theory.

**Gate:** the new capability can be installed/disabled without changes to core orchestration beyond its generic contracts. Commit: `feat(extensions): add second capability adapter`.

## M10 — Evaluation and hardening

**Outcome:** the project progresses by measured behavior, not anecdotes.

### M10.1 — Lock a regression request set
- Cover understanding, clarification, tool selection, state updates, tool errors, permissions, file preservation, and output grounding.

**Gate:** fixtures are version-controlled with explanations for expected results. Commit: `test(agent): establish regression benchmark`.

### M10.2 — Add loop/resource safeguards
- Enforce maximum steps, repeated-action detection, per-tool timeouts, cancellation, and observable budget exhaustion.

**Gate:** tests prove all limits stop the agent safely. Commit: `feat(agent): enforce execution budgets`.

### M10.3 — Run adversarial tool-output tests
- Test attempts by file/web/tool text to override system policy or trigger unauthorized actions.

**Gate:** untrusted content cannot bypass executor permissions. Commit: `test(security): cover untrusted tool-output handling`.

### M10.4 — Write a capability matrix and known limitations
- Document exactly what requests are supported, partially supported, or unsupported.
- Use failures to prioritize the next milestone rather than broadening claims.

**Gate:** each advertised capability points to test coverage or a clearly marked manual limitation. Commit: `docs: publish capability and limitation matrix`.

---

## Milestone exit rule

A milestone is complete only when every substep's acceptance gate is met, the tests pass, and the result is committed. If a gate fails, fix or revert the current substep before proceeding. A commit is not proof of correctness; the test output and review are.
