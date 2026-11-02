# Lattice

A research prototype of a symbolic, tool-using agent built around explicit state and inspectable components — not a trained generative model.

Lattice represents goals, facts, constraints, rules, and tool results as typed data. The repository contains separate, bounded components; the capability matrix describes what is integrated, what is only an optional API, and what remains unsupported.

> ⚠️ **Status:** research code, not a product. The CLI demo accepts controlled statements and registers only the calculator. Other tools and adapters require explicit host composition; see the [capability matrix](docs/CAPABILITIES.md).

## 🔁 Intended turn loop

The following is the architectural direction, **not a fully wired default runtime path**:

1. Parse input into a structured statement, or map an ordinary request onto a known intent — and say so when nothing matches.
2. Update session state immutably, keeping provenance for every fact.
3. Retrieve relevant package knowledge and derive new facts with rules.
4. Propose the next action; validate it against schema, policy and current state.
5. Execute the tool, record the result, update the plan, repeat within budgets.
6. Realize an answer, an edit, or a stop signal.

## ✍️ Statements

The formal path accepts one statement per line, fields split by `|`, and rejects anything ambiguous rather than guessing:

```text
fact water | boils_at | 100C
goal Summarize the file. | A summary exists.
constraint Do not change other sections.
action calculator | left=2 | right=3
```

The library includes a separate, deliberately small [pattern-based interpreter](docs/NATURAL_LANGUAGE.md) that stays silent when it doesn't match; the demo CLI currently accepts controlled statements only and does not route ordinary requests through that interpreter.

## 🚀 Get started

Requires the .NET 10 SDK (`dotnet --version`); all projects target `net10.0` via `Directory.Build.props`.

```sh
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Run the REPL (`--debug` adds a per-turn trace):

```sh
dotnet run --project Lattice.Cli
```

```text
Lattice demo. Controlled statements: fact | goal | constraint | action. Type 'exit' to quit.
> fact water | boils_at | 100C
> action calculator | left=2 | right=3
```

Build output goes to `artifacts/`, which is git-ignored.

## 🛠️ Tools

| Tool | Behaviour |
|---|---|
| `calculator` | arithmetic; the one tool the CLI registers today |
| `files.search`, `files.read`, `files.list` | read-only workspace inspection |
| `files.write`, `markdown.edit` | edits; refused when byte encoding or BOM can't be preserved |
| `command.run` | allowlisted verification commands |
| `csharp.syntax` | Roslyn diagnostics over C# source text |
| `python.syntax` | Optional Python AST syntax check; requires a configured Python interpreter |

These are implementations available for explicit host composition, not a list of tools enabled by the CLI. `ToolExecutor` validates arguments and enforces permissions before invocation. There is no general semantic validator for tool outputs: results are recorded as data, and callers must treat them as untrusted. The demo CLI uses a read-only policy.

## 🗂️ Layout

| Path | Contents |
|---|---|
| `Lattice/` | `Lattice.Core` — domain types, agent loop, tools, knowledge and retrieval |
| `Lattice.CSharp/` | `Lattice.CSharp` — optional syntax/semantic inspection and narrow source edit API |
| `Lattice.Python/` | `Lattice.Python` — optional Python AST syntax adapter; requires a configured interpreter |
| `Lattice.Cli/` | console REPL; composition root, no domain logic |
| `Lattice.Core.Tests/` | xunit tests, corpora and fixtures |
| `knowledge/` | example knowledge package (`package.json`) |
| `config/nlp/` | intent patterns |
| `docs/` | specifications and decision records |

## 📚 Docs

| Document | Covers |
|---|---|
| [PRD](docs/PRD.md) | vision, thesis, target use cases |
| [Architecture](docs/ARCHITECTURE.md) | components and boundaries |
| [Roadmap](docs/ROADMAP.md) | milestones M0–M10, each split into gated substeps |
| [Decisions](docs/DECISIONS.md) | ADRs, with reasons |
| [Controlled language](docs/CONTROLLED_LANGUAGE.md) | statement grammar and value typing |
| [Natural language](docs/NATURAL_LANGUAGE.md) | bounded intent patterns and their limits |
| [Knowledge packages](docs/KNOWLEDGE_PACKAGE.md) | data format; packages are never executed |
| [Retrieval benchmark](docs/RETRIEVAL_BENCHMARK.md) | query set, metrics, measured baseline |
| [M8 design](docs/M8_DESIGN.md) | prose and document editing invariants |
| [Capability matrix](docs/CAPABILITIES.md) | implemented scope, test references, and known limitations |
| [Working protocol](docs/AI_WORK_PROTOCOL.md) | how changes get scoped and reviewed |

## 📌 Working rules

- One small, reversible increment at a time; it must build, test and commit on its own.
- Claims stay narrow: unsupported input is rejected or questioned, never guessed at.
- Where code and docs disagree, the code is the truth and the doc gets corrected.
