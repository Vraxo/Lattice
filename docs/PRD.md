# Product Requirements Document — Project Lattice

**Status:** Draft v0.1  
**Date:** 2026-10-09  
**Working name:** Lattice

## 1. Product vision

Build an inspectable, extensible, tool-using agent whose primary intelligence comes from explicit state, symbolic representations, a knowledge system, deterministic inference, planning, retrieval, and feedback from tools—not from a trained generative neural model.

The agent should accept ordinary user requests, determine what is known and what remains uncertain, plan and execute actions, ask useful clarification questions when necessary, and communicate results in natural language. It should be useful both for software work and ordinary text work.

The first version must be deliberately narrow. The product grows through verified capabilities, not through an enormous up-front ontology or an untestable promise of universal language understanding.

## 2. Intended use cases

Long-term target capabilities:

- Coding agent: inspect, explain, review, modify, debug, test, and refactor projects.
- Initial ecosystem: C#, .NET, Razor, CSS, XML, Markdown, and related tooling; later extend to any ecosystem with adequate knowledge packages and capability adapters.
- General tool use: calculator, clock, file system, shell, Python, web search, HTTP/API tools, databases, MCP-backed tools, and future registered capabilities.
- Conversation: answer questions, explain decisions/results, discuss options, give qualified opinions, ask clarification questions, and maintain relevant conversation state.
- Prose editing: edit Markdown and other text while preserving structure and honoring user constraints.
- Planning: decompose tasks, track dependencies and completion criteria, respond to tool feedback, revise a plan, and stop when done.

## 3. Core product thesis

The core agent should primarily select and manipulate **structured states and actions**, rather than generating every internal decision as free-form prose. Natural language remains important at the input and output boundaries and may be used in intermediate representations when useful, but it is not the only representation.

A typical control loop:

1. Ingest the user request and current conversation/tool context.
2. Build or update a structured context snapshot.
3. Extract candidate intents, entities, goals, constraints, and unknowns.
4. Retrieve relevant package knowledge, rules, examples, and observations.
5. Choose one next operation: ask, inspect, plan, invoke a tool, edit, explain, or finish.
6. Validate the proposed operation against its schema, policy, permissions, and current state.
7. Execute it when authorized and add the result—with provenance—to state.
8. Evaluate progress, update the plan, and repeat within budgets.
9. Realize a user-facing response or artifact and report verified outcomes.

## 4. Product requirements

### P0 — Foundation

- The solution builds and tests locally with one documented command sequence.
- Core logic is usable without a GUI.
- Public models and interfaces are small, typed, and independently testable.
- Configuration and knowledge data live outside engine logic wherever practical.
- Failures use explicit result/error values for expected operational failures; exceptions are reserved for unexpected faults and boundaries.

### P1 — Explicit state and evidence

The engine can represent at minimum:

- User request and conversation messages.
- Goals, subgoals, constraints, and completion criteria.
- Entities and references between entities.
- Facts/observations with source, timestamp or turn, confidence/status where meaningful, and provenance.
- Assumptions distinct from verified facts.
- Available tools/capabilities and their schemas.
- Proposed, approved, running, completed, failed, and rejected actions.
- Tool outputs and links from outputs to conclusions that use them.

State must be inspectable in debugging output. Avoid building a grand universal world ontology in the first version.

### P2 — Typed tools and action loop

- Tools register a stable identifier, description, input schema, output contract, and policy metadata.
- The engine can validate, propose, invoke, record, and recover from tool calls.
- The first tools are deliberately harmless and deterministic (for example, a calculator and a test/fake tool); file access comes later.
- Tool failures, invalid arguments, timeouts, cancellation, and malformed results are explicit states.
- Tool output is untrusted data, not a new source of governing instructions.

### P3 — Extensible knowledge

- Knowledge can be loaded from external, versioned packages.
- A package can declare concepts, relations, facts, rules, vocabulary/aliases, syntax patterns, examples, constraints, and supported capabilities.
- The core queries package interfaces rather than branching on names such as `CSharp`, `Razor`, or `MCP`.
- Code that must call a compiler, parser, or external API can be supplied as a registered adapter; declarative files alone are not claimed to implement arbitrary external behavior.
- Package validation reports missing references, incompatible schema versions, duplicate identifiers, and invalid rules before activation.

### P4 — Controlled language and natural-language intake

- Start with a small explicit grammar for expressing facts, goals, constraints, and action requests.
- Use ACE (Attempto Controlled English) as a research reference for unambiguous English-like formal statements, not as a requirement that all users speak ACE.
- Natural-language intake improves incrementally through lexical lookup, syntactic patterns, intent/slot extraction, context resolution, retrieval, and ambiguity detection.
- If interpretation is materially ambiguous, the engine asks a focused question rather than silently inventing requirements.
- Parsed meaning includes confidence/status and links to the source span/message when possible.

### P5 — Planning and execution

- Represent tasks as goals with optional subgoals, preconditions, expected effects, dependencies, and completion tests.
- Plans are inspectable and revisable after new observations.
- The engine chooses the next useful action from eligible candidates; it does not need to express every intermediate decision as natural-language prose.
- Stop when completion criteria are satisfied, when user input is needed, or when a budget/policy prevents progress.
- Record concise decision justifications (selected action, relevant facts/rules, rejected alternatives where useful), not an unbounded free-form pseudo-thought transcript.

### P6 — Output and surface realization

- Provide natural-language answers using explicit sentence/phrase templates and grammar-aware realization where feasible.
- Code edits prefer structured edits, ASTs, syntax-aware transformations, patches, formatters, and serializers over unconstrained whole-file re-generation.
- Markdown editing should preserve document structure and unchanged sections whenever possible.
- Separate semantic intent from wording so a response can be rephrased without changing the facts or action plan.
- Support a graceful fallback: when the engine cannot reliably express or interpret something, it should say so or ask a question.

### P7 — Coding capability

- Discover files and project structure using registered file tools.
- Search code and knowledge.
- Read and summarize relevant files, with file/line provenance.
- Use a language adapter for syntax-aware inspection; begin with Roslyn for C#.
- Propose targeted patches and show diffs before writing.
- After authorized changes, run configured build/tests and use diagnostics as structured observations.
- Never claim a build, test, or edit succeeded without tool evidence.

### P8 — Safety, user control, and reliability

- Read-only tools and write-capable tools have distinct permissions.
- Destructive operations, external side effects, command execution, and broad edits require explicit policy/consent appropriate to the action.
- Provide dry-run or preview for file modifications where practical.
- Bound tool time, iteration count, context size, and total execution budget.
- Support cancellation and leave recoverable state after failure.
- Log action arguments, result status, provenance, and file diffs while allowing sensitive output to be redacted.
- Never execute arbitrary text as code merely because it appeared in a tool result or knowledge file.

## 5. Non-goals for the initial MVP

- Reproducing GPT-2/GPT-3 general conversational fluency.
- Building a neural model, training corpus, gradient-based training loop, or neural text generator.
- Building a general-purpose theorem prover or complete representation of the real world.
- Supporting every natural-language construction or every programming language on day one.
- Automatically trusting all actions or executing arbitrary shell commands without policy checks.
- GUI, cloud service, multi-agent orchestration, long-term personal memory, or automatic online knowledge ingestion before the basic loop is stable.

## 6. Constraints and design policies

- Initial implementation: C#/.NET, headless first.
- Keep runtime dependencies minimal and explicitly justified.
- No custom ML training or learned generative model in the core.
- Frozen pretrained embeddings may be considered only as an optional later retrieval feature. They must not be required for the MVP or treated as a substitute for explicit logic.
- Corpus frequencies, collocation indexes, and other non-neural resources may be added when they have a concrete evaluation use case and provenance/licensing are understood.
- New language/framework support should be data- or adapter-driven; no language-name switchboards in core control flow.
- Add abstractions only when a real use case requires them. Keep one class per file, use spaces rather than tabs, and avoid needless private nested classes or comments that merely narrate obvious code.

## 7. Definition of MVP

A first useful demonstration should be able to:

1. Start a conversation and maintain a structured, inspectable session state.
2. Parse a bounded set of explicit requests/controlled-language statements.
3. Retrieve a fact/rule from a small external knowledge package.
4. Decide to call a registered deterministic tool using a typed schema.
5. validate the call, execute it, and record the result with provenance.
6. Use that result to select a next step or construct an answer.
7. Ask for clarification when a required value is missing or ambiguous.
8. Pass automated tests that cover the happy path, invalid input, tool failure, and unknown intent.

A second demonstration should inspect a small local project, search/read files, propose a patch, and run a safe verification command only after the user approves the write/command policy.

## 8. Success metrics

Track measurable, small-scope indicators rather than subjective claims of intelligence:

- Unit/integration test pass rate on committed fixtures.
- Percentage of benchmark requests interpreted into the intended structured goal.
- Correct tool choice and schema-valid call rate.
- Task completion rate for a fixed set of reproducible tasks.
- Clarification precision: asks when ambiguity matters and does not ask when the required facts are already present.
- Unsupported-claim rate: outputs claiming success without matching evidence (target: zero in the test suite).
- Unintended file-change rate (target: zero in the test suite).
- Ability to add a new tool/package without editing core orchestration.
- Ability to disable optional subsystems without breaking the core.

Maintain a versioned, human-readable evaluation set. Do not change its expected answers casually to make regressions disappear.

## 9. Open decisions to defer until evidence exists

- Exact declarative package format: choose after one real package and validation tests exist.
- General-purpose formal inference engine: begin with a small typed rule interface; adopt a larger solver only if needed.
- Persistent database: begin with simple files/in-memory state; add a database only when scale or query needs justify it.
- Embeddings: benchmark an optional off-the-shelf embedder against lexical/symbolic retrieval first.
- Free-form natural-language generation strategy: establish a small baseline and evaluate; do not over-design a universal grammar before collecting examples.
