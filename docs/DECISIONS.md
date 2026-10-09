# Architecture Decision Records — Initial Set

These are starting decisions, not sacred rules. A decision should change only when implementation evidence gives us a concrete reason, and the change should be recorded here.

## ADR-001 — Symbolic agent first; no trained generative model in core

**Decision:** The core agent uses explicit state, rules, retrieval, planning, typed actions, and tool feedback. No neural generative model or custom training pipeline is part of the core.

**Allowed for later evaluation:** frozen pretrained embeddings as an optional retrieval provider, provided the project remains useful without them and no model is fine-tuned. Do not make embeddings the planner or source of truth.

**Reason:** Preserves the project's central research goal and keeps the reasoning path inspectable.

## ADR-002 — Structured action/state over prose as the control interface

**Decision:** Internal goals, tool calls, state transitions, and code edits use typed structures wherever practical. Natural language is primarily the user interface, plus one optional representation among others.

**Reason:** Structured actions can be schema-validated, permission-checked, tested, and verified without repeatedly reinterpreting generated prose.

## ADR-003 — Controlled English is a formal sub-language, not the only user interface

**Decision:** Use ACE as a reference and consider a small ACE-inspired subset for facts, rules, goals, constraints, and test fixtures. Ordinary user requests remain natural-language input and require a separate interpretation path.

**Reason:** Controlled language is useful where ambiguity must be low, but forcing it onto every user would undermine the intended assistant experience.

## ADR-004 — Start headless and dependency-light

**Decision:** Begin with a .NET/C# core library, CLI, and tests. Delay GUI and broad service integration until a complete agent loop exists.

**Reason:** Reduces unrelated complexity and makes each capability testable.

## ADR-005 — Use native parsers/compilers through adapters

**Decision:** Use mature ecosystem tools such as Roslyn for C# parsing and semantic analysis. Keep such dependencies behind adapters rather than putting them into the core domain model.

**Reason:** Compiler-grade tooling is more reliable than custom parsers and gives the agent verifiable structured facts.

## ADR-006 — Packages define knowledge; adapters implement executable capabilities

**Decision:** Declarative packages contain facts, vocabulary, rules, APIs, examples, and metadata. Operations requiring executable behavior are supplied by registered adapters. Both are discoverable through generic interfaces; core orchestration should not be hard-coded for package names.

**Reason:** Extensibility must be real, but data files should not be expected to implement arbitrary external behavior by themselves.

## ADR-007 — Permission checks happen at execution time

**Decision:** Every side-effecting capability is checked centrally at the executor boundary. Planner intent alone never authorizes execution.

**Reason:** A mistaken plan or hostile tool output must not bypass user control.

## ADR-008 — Measure capability using fixed fixtures

**Decision:** Maintain a versioned benchmark set with expected interpretation, tool choice, state transition, and outcome. Run it throughout development.

**Reason:** Prevents anecdotal demos from hiding regressions and makes architectural trade-offs measurable.
