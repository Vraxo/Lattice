# Regression request set

`v1.json` is a small, versioned end-to-end regression set for understanding, clarification, tool selection, state updates, tool failures, permissions, file preservation, and evidence-grounded answers. Every entry has a stable id, input, explicit expected result, and rationale.

The corpus is copied into test output by the `tests/corpus/**/*.json` rule in `Lattice.Core.Tests.csproj`. `RegressionRequestSetTests` executes each entry through the relevant existing public boundary; it does not add production behavior.

Expected values are deliberate behavior contracts, not disposable snapshots. Change one only with an intentional behavior change, review of the affected contract, an updated rationale, and test evidence. The fixture's `changePolicy` field repeats this rule next to the data.
