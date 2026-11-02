# Adversarial tool-output corpus

`v1.json` contains fixed file-, web-, and tool-originated text that attempts to override policy or induce a write. The web and generic tool sources are offline test doubles; no network access or production tool behavior is introduced.

`UntrustedToolOutputTests` returns each payload through the relevant read-only tool boundary, asserts it remains raw invocation output rather than facts, goals, observations, or an automatically executed control statement, then attempts the write explicitly under the read-only policy and verifies executor denial.

Treat payloads and expected outcomes as security contracts. Update them only alongside an intentional, reviewed behavior change and rationale.
