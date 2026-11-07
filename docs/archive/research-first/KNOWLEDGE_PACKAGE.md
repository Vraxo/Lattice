# Knowledge Package Format
**Status:** v1.0 draft (M3.1)
## 1. Purpose
A knowledge package is external, versioned **data** describing concepts, aliases, facts, and rules. Packages are never executed; executable behaviour is supplied by registered adapters (ADR-006).
## 2. Format
Single JSON file per package, parsed with `System.Text.Json`. No third-party parser dependency (ADR-004, ADR-009).
Layout: one directory per package, named after the package id, containing `package.json`.
```
knowledge/
  example.physics/
    package.json
```
## 3. Top-level object
| Field | Type | Required | Notes |
|---|---|---|---|
| `schemaVersion` | string | yes | `"MAJOR.MINOR"` |
| `id` | string | yes | package identifier |
| `name` | string | yes | human-readable name |
| `description` | string | no | |
| `concepts` | array | no | see §6 |
| `facts` | array | no | see §6 |
| `rules` | array | no | see §6 |
Unknown top-level fields are rejected (fail closed). See §7.
## 4. Schema versioning policy
- `schemaVersion` is `"MAJOR.MINOR"`, for example `"1.0"`.
- The loader supports MAJOR version `1`. A package with a different MAJOR is rejected.
- A MINOR greater than or equal to the loader's supported MINOR is accepted. Because a MINOR bump may only add optional fields, a newer package remains parseable.
- Unknown fields are rejected (fail closed), so a package cannot pass behaviour to the loader through an unrecognized key.
- Adding an optional field is a MINOR bump. Removing, renaming, or retyping a field is a MAJOR bump.
- No migration framework in the MVP.
## 5. Identifier rules
- Applies to the package `id` and every entity `id`.
- Pattern: `^[a-z0-9]([a-z0-9._-]*[a-z0-9])?$`, max 128 characters. Lowercase alphanumerics plus `.`, `_`, `-`; must start and end alphanumeric.
- Entity ids are unique within the package **across all entity kinds** (concept, fact, rule).
- Convention, not enforced: prefix by kind — `concept.`, `fact.`, `rule.`.
- References between entities are **package-local** in v1. Cross-package references are deferred (§9).
## 6. Entities
### Concept
| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | |
| `preferred` | string | yes | canonical display name, non-empty |
| `aliases` | array of string | no | each non-empty |
| `definition` | string | no | |
### Fact
A subject–predicate–value triple. `value` is a string in v1; typed values are deferred (§9).
| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | |
| `subject` | string | yes | must reference a concept `id` in this package |
| `predicate` | string | yes | non-empty |
| `value` | string | yes | non-empty |
| `source` | string | no | human provenance note |
### Rule
Derives one fact pattern from one or more premise fact patterns. v1 supports **pattern matching only** (no numeric comparison, no negation).
| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | |
| `description` | string | yes | non-empty |
| `premises` | array of Pattern | yes | at least one |
| `conclusion` | Pattern | yes | |
### Pattern
| Field | Type | Required | Notes |
|---|---|---|---|
| `subject` | string | yes | must reference a concept `id` in this package |
| `predicate` | string | yes | non-empty |
| `value` | string | yes | non-empty |
## 7. Validation (implemented in M3.2)
The loader reports all actionable problems before activation:
- malformed JSON;
- unsupported `schemaVersion` MAJOR;
- missing required fields, or unknown fields;
- identifier pattern violations;
- duplicate entity ids (within and across kinds);
- `subject` references to concepts not defined in the package;
- empty `premises`, empty `aliases` entries, or empty strings in required string fields.
A package that fails validation is not activated.
## 8. Conflict policy
Deferred. v1 loads a single package; multi-package conflict resolution is not yet defined.
## 9. Out of scope for v1
- a separate `relations` section (relations are folded into facts as subject–predicate–value for now);
- numeric or typed fact values;
- comparisons, negation, or recursion in rules;
- cross-package references;
- syntax patterns, grammars, examples, and capability metadata (later milestones);
- schema migrations.
Each deferred item is recorded so it is not silently assumed.