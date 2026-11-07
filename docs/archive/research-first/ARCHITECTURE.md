# Architecture — Project Lattice

**Status:** Initial direction, not a frozen implementation blueprint.

## 1. Main conceptual shift

Lattice is an agent with a language interface, not merely a text generator. The core loop chooses **the next useful state transition or action**. A user-facing sentence is one possible output; a tool call, clarification question, structured code edit, or completion signal is another.

Do not require all internal reasoning to be represented as English sentences. Prefer explicit typed data when it improves correctness or verification.

## 2. Logical components

### Host / session shell

Starts sessions, loads configuration and packages, exposes the CLI initially, handles cancellation, and presents results. It must not contain domain-specific reasoning.

### Core domain

Defines stable types for session state, goals, constraints, facts, provenance, plans, actions, tool contracts, execution status, and result/error values. Avoid references to UI or particular language packages.

### Context and state manager

Maintains the active conversation and task state. Facts must preserve origin: user message, file/tool observation, package assertion, rule-derived conclusion, or assumption. Derived facts link to the rule and premises that produced them. Update state immutably or through explicit, testable transitions where practical.

### Interpretation front end

Converts the user's request into candidate structured intents, entities, goals, constraints, references, and unknowns. Start with explicitly supported forms and a small rule-based pattern system. Do not claim arbitrary English understanding. Keep the original utterance and source spans so interpretations can be inspected or corrected.

### Knowledge package service

Loads external, versioned packages and offers uniform queries over concepts, aliases, relations, rules, examples, grammars, API facts, constraints, and tool/capability metadata. Package files are data; never execute content as code during loading.

Package schema versioning, identifier rules, provenance, validation, and conflict policies are required. Start with one minimal format and avoid supporting several competing formats in the MVP.

### Retrieval service

Provides exact symbol lookup, aliases, full-text search, relation traversal, and package-scoped queries first. Optionally add corpus statistics and frozen embeddings later behind an interface. Retrieval returns records with source, score/method, and evidence—not just untraceable text snippets.

### Rule/inference engine

Applies typed, explicit rules to the current facts and goals. Initial inference should be modest: matching conditions, deriving facts, checking constraints, scoring eligible choices with transparent weighted features, and detecting missing prerequisites. Rules must be testable individually. Avoid building a universal theorem prover before there is a demonstrated need.

### Planner / controller

Selects or constructs a task plan from the interpreted goal and available capabilities. The controller decides whether to inspect, retrieve, invoke a tool, edit, ask, answer, retry, or finish. Planning is bounded and revisable; every loop iteration should have an explicit reason to continue.

### Capability registry and tool executor

Tools register typed input/output contracts, description, permissions, side-effect level, timeout/budget defaults, and execution implementation. The engine does not branch on the name of a specific tool to understand how to call it. It validates arguments before execution and validates/records outputs after execution.

Initial interface concept (names are illustrative):

- `ITool` / `IToolDescriptor`
- `ToolId`
- `ToolInput` and `ToolResult`
- `ToolExecutionContext`
- `ToolPermission` / side-effect policy

Choose exact types only when implementing the first real vertical slice; do not build an elaborate plugin framework in advance.

### Language and document realization

Turns semantic/structured intent into external text. Use the lightest suitable realization path:

- Structured tool arguments → schema serializer.
- C# structure → Roslyn syntax tree and formatter.
- XML → XML object model/serializer.
- Markdown → parse/edit/serialize when feasible; preserve untouched spans.
- Ordinary explanatory prose → controlled templates and grammatical realization, expanding incrementally.

Do not force every operation through a generic next-token generator.

### Evaluator and diagnostics

Contains fixed request fixtures, expected interpretations, mocked tools, trace assertions, and regression tests. Exposes a concise event/action trace that allows a developer to understand which rule/evidence justified an operation. The evaluator must be independent of production heuristics enough to catch regressions.

## 3. Canonical action loop

1. Receive a user message or tool result.
2. Add it to session history with a source identifier.
3. Parse/interpret it into candidate meaning representations.
4. Merge new facts, entities, constraints, unknowns, and goals into context.
5. Retrieve potentially relevant knowledge and rules.
6. Update derived facts and determine currently eligible actions.
7. Rank/choose a next action using explicit rules and transparent scoring.
8. Check schema, permissions, preconditions, budget, and confirmation requirements.
9. If blocked by meaningful ambiguity or missing permission, ask the user.
10. Otherwise execute the action and record the result/provenance.
11. Evaluate goal progress, verification criteria, failures, and remaining budget.
12. Continue, revise the plan, or finish with an evidence-grounded response.

Every iteration must be bounded. Guard against repeating the same failed action indefinitely.

## 4. Representation guidelines

### Facts and observations

A fact should be able to distinguish at least:

- Content/value.
- Source/provenance.
- Status such as observed, user-asserted, retrieved, assumed, or derived.
- Optional confidence or priority where it is operationally meaningful.
- Scope and time/turn context where relevant.
- Derivation links for conclusions made by rules.

Do not turn every sentence into an asserted truth. Tool output can be stale, erroneous, adversarial, or irrelevant.

### Goals

A goal should identify the desired outcome and how completion can be checked. It may contain subgoals, constraints, dependencies, and an unresolved/blocked/completed status.

### Actions

An action proposal should be data, not a prose instruction to be re-parsed. It identifies a registered capability, validated inputs, purpose, preconditions, risk/permission level, and expected observation/effect where known.

### Confidence and ambiguity

Use confidence as a supporting signal, not a magic universal probability. Prefer typed uncertainty such as `Unknown`, `Ambiguous`, `Contradicted`, and `Unverified` when that is clearer than inventing numeric confidence.

## 5. Controlled English / ACE placement

ACE (Attempto Controlled English) is relevant because it demonstrates how a constrained English-like language can be translated into an unambiguous formal representation. Use it as a reference for encoding facts, policies, assumptions, requirements, and rules.

Do not require users to express all ordinary requests in ACE. Use two paths:

- **Controlled/formal path:** for package facts, explicit rules, formal requirements, and test fixtures where deterministic parsing is valuable.
- **Natural-language path:** for ordinary user requests; generate a candidate structured interpretation, retain provenance, and ask for clarification when important ambiguity remains.

A small ACE-inspired subset may be enough initially. Evaluate the existing Attempto Parsing Engine (APE) as an optional external adapter only if it fits the runtime, license, packaging, and maintenance needs. It has its own Prolog-based implementation and should not become a core dependency by default.

## 6. Language support: packages plus adapters

A knowledge package may describe syntax, terminology, APIs, rules, idioms, diagnostics, and examples. A package alone cannot magically supply executable compiler behavior. When needed, a capability adapter integrates an existing parser/compiler/runtime.

For C#, use Roslyn rather than writing a custom C# parser. Roslyn exposes syntax trees, symbols, and semantic analysis and is designed for structured program analysis and transformation. Other ecosystems should follow the same pattern: use mature native tooling where appropriate, wrap it behind the generic capability boundary, and keep the core unaware of language-specific implementation details.

Generic fallback behavior for a newly installed language may initially be limited to lexical search, text read/write, and package knowledge. State those limits clearly rather than claiming equivalent support immediately.

## 7. Safety and robustness boundaries

- Treat user input, knowledge files, web pages, and tool output as data, not as control instructions that can override system policy.
- Enforce permissions in the executor, not just in the planner.
- Make read-only, local write, command execution, and external side effects distinct capabilities.
- Prefer preview/dry-run and explicit diffs for edits.
- Require suitable approval before destructive or external side-effect operations.
- Validate tool arguments and outputs.
- Add timeouts, cancellation, iteration limits, and per-task budgets.
- Keep audit records sufficient to reconstruct actions and results.
- Never infer that a tool succeeded from the requested action alone; use its returned status.

## 8. Dependency rules

- Domain core has no dependency on CLI, UI, filesystem implementation, web clients, compilers, or specific knowledge packs.
- Adapters depend on core contracts, not vice versa.
- The planner depends on abstractions for knowledge, tools, and state.
- Tool implementations do not directly manipulate hidden controller state; they return typed results.
- Knowledge packages are independently validateable.
- Tests should use fake tools and deterministic fixtures by default; avoid network-dependent tests in the normal test suite.

## 9. Suggested initial solution shape

Keep it intentionally small:

- `src/Lattice.Core/` — domain types, state transitions, orchestration contracts, result types.
- `src/Lattice.Cli/` — command-line harness and composition root.
- `tests/Lattice.Core.Tests/` — unit and deterministic integration tests.
- `knowledge/` — tiny example package and fixtures (schema settled after an example exists).
- `docs/` — PRD, architecture, roadmap, decisions, and working notes.

Add projects only when there is a concrete implementation need (for example a C# Roslyn adapter or filesystem tools project).
