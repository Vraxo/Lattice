# Project Lattice
**Working name:** Lattice
**Tagline:** A symbolic, tool-using agent built around explicit state, knowledge, and actions.
Lattice is an experiment in building a useful general-purpose agent without training a generative neural model. It maintains explicit context and structured state, retrieves knowledge, applies rules, plans actions, invokes registered tools, checks results, and realizes answers or edits as text when needed.
See the [Product Requirements Document](docs/PRD.md), [Architecture](docs/ARCHITECTURE.md), [Roadmap](docs/ROADMAP.md), and [Architecture Decisions](docs/DECISIONS.md) for direction.
## Requirements
- .NET SDK targeting `net10.0` (all projects share this target via `Directory.Build.props`).
## Solution layout
- `Lattice/` — `Lattice.Core` class library (domain types, orchestration contracts).
- `Lattice.Cli/` — `Lattice.Cli` console harness (composition root).
- `Lattice.Core.Tests/` — xunit test project.
- `docs/` — PRD, architecture, roadmap, decisions, and working protocol.
## Build and test
Run from the repository root:
```sh
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```
Build output is redirected to `artifacts/` (configured by `Directory.Build.props` via `UseArtifactsOutput`), which is git-ignored.