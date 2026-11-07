# Lattice

Lattice is a **symbolic, tool-using coding assistant** built around explicit state, bounded actions, and evidence from the user's workspace. It does not use an LLM or a trained generative model.

## Project direction

Lattice is moving from a research-first codebase to a **usable desktop application, built in small vertical increments**. The existing Core, tools, adapters, and tests are being retained; the immediate task is to connect the useful pieces to a real interface rather than expand the architecture in isolation.

> **Current status:** the desktop application is the active product target, but `Lattice.Desktop` has not been implemented yet. The existing console project is a limited development harness, not the intended user experience.

The first desktop release is intentionally narrow: open a local workspace, inspect a file, and find source-text occurrences with file-and-line evidence. It is read-only. It will not pretend that literal text search is compiler-grade symbol resolution.

Start implementation from the **[active TODO queue](.agents/todo.md)**. It contains the ordered, checkable tasks. The [desktop direction](docs/DESKTOP_DIRECTION.md) explains the product target; coding agents must follow the repository-root [AGENTS.md](AGENTS.md).

## Existing code

The repository already contains reusable pieces:

| Project | Purpose |
|---|---|
| `Lattice/` | `Lattice.Core` — domain types, agent loop, workspace tools, knowledge and retrieval |
| `Lattice.CSharp/` | Optional Roslyn syntax/semantic inspection and a narrow syntax-edit API |
| `Lattice.Python/` | Optional Python AST syntax adapter |
| `Lattice.Cli/` | Limited controlled-statement console harness |
| `Lattice.Core.Tests/` | Core tests, fixtures, and evaluation corpora |
| `knowledge/`, `config/`, `tests/` | Example knowledge package, intent patterns, and test data |

The desktop host will be added as `Lattice.Desktop/`. UI and desktop-specific dependencies must stay out of `Lattice.Core`.

## Build and test

Requires the .NET 10 SDK.

```sh
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

The current console harness can still be run with:

```sh
dotnet run --project Lattice.Cli
```

It accepts controlled statements and registers only the calculator. It is not the desktop app and does not yet provide the intended workspace-chat workflow.

## Documentation

- [Active implementation TODO](.agents/todo.md) — ordered tasks and acceptance checkboxes.
- [Desktop product direction and first acceptance target](docs/DESKTOP_DIRECTION.md)
- [Documentation index](docs/README.md)
- [Historical research-first specifications and milestone records](docs/archive/research-first/)

The archived documents are retained for context. They are **not** the active roadmap or instructions for new work.
