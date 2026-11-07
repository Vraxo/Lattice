# Controlled Statement Subset — Lattice Mini-Language
**Status:** v1.0 draft (M4.1)
**Language version:** `1.0`
## 1. Purpose
This document specifies the small, controlled statement language Lattice accepts on its **formal path**: facts, goals, constraints, and explicit action requests. It is the language used for package rules, test fixtures, and any input where deterministic parsing is worth more than natural phrasing.
It is deliberately **not** general English. The natural-language path for ordinary requests is separate (M4.3) and is expected to be less certain, to retain provenance, and to ask clarifying questions. Do not conflate the two.
## 2. Relationship to ACE
ACE (Attempto Controlled English) is the reference for the *idea*: a constrained, English-adjacent language that maps cleanly to a formal representation. This subset borrows the idea, not the syntax. It makes no claim of ACE compatibility, and no attempt is made to parse arbitrary ACE.
## 3. Design rules
1. **One statement per line.** The newline is the statement delimiter. No trailing period is required or stripped.
2. **Fields are separated by `|`.** Leading and trailing whitespace around each field is trimmed; internal whitespace is preserved.
3. **The first token is a keyword**, case-sensitive, from a fixed set: `fact`, `goal`, `constraint`, `action`.
4. **Reject, never guess.** An input that could be read more than one way is rejected with a specific error. The engine does not silently choose an interpretation.
5. **No free-form reasoning is expressed here.** This language carries structured statements only.
## 4. Statement forms
```
statement  := fact | goal | constraint | action
fact       := "fact"       "|" subject "|" predicate "|" value
goal       := "goal"       "|" description "|" completion
constraint := "constraint" "|" description
action     := "action"     "|" toolId ( "|" argument )*
argument   := name "=" value
```
Each field's content is free text with the exceptions noted below (identifiers, argument values).
### 4.1 `fact`
Asserts that `subject`–`predicate`–`value` holds.
- `subject`, `predicate`, `value`: non-empty after trimming.
- The value may contain spaces and periods.
- Does **not** define the subject concept; unknown subjects are not resolved here.
Example: `fact water | boils_at | 100C`
### 4.2 `goal`
Declares a goal and its completion condition.
- `description`: non-empty.
- `completion`: non-empty. A goal with no completion condition is **rejected** (see §9); the language does not invent one.
Example: `goal Summarize the file. | A summary exists.`
### 4.3 `constraint`
Declares a constraint on the current task.
- `description`: non-empty.
Example: `constraint Do not change other sections.`
### 4.4 `action`
Requests invocation of a registered tool by id, with typed arguments.
- `toolId`: non-empty, matching `^[A-Za-z0-9._-]+$`.
- `argument`: `name=value`, where `name` is non-empty and `value` is typed per §5.
- Zero arguments is valid: `action clock`.
- Duplicate argument names are **rejected**.
Examples:
```
action calculator | left=2 | right=3
action echo | message=hello world
action flag | enabled=true
```
## 5. Value typing (action arguments)
The text after `=` is typed by shape:
| Form | Type | Examples |
|---|---|---|
| `true` / `false`, case-insensitive | Boolean | `true`, `FALSE` |
| optional `-`, one or more ASCII digits, nothing else | Integer | `2`, `-7`, `0` |
| optional `-`, digits, a `.`, digits (mandatory point, no exponent) | Number | `1.5`, `-0.25` |
| anything else, non-empty | String | `hello world`, `100C`, `2x` |
An empty value (`name=`) is malformed, not an empty string.
## 6. Meaning representation
A successfully parsed statement yields one of:
```
Fact       { Subject, Predicate, Value }
Goal       { Description, Completion }
Constraint { Description }
Action     { ToolId, Arguments: [ { Name, Type, Value } ] }
```
This is a **syntax/meaning** record, not a domain object. Mapping these onto `Fact`, `Goal`, `Constraint`, `ToolInvocation`, etc. is a later concern; the parser's job (M4.2) is to produce inspectable structure with source spans and to reject malformed input.
## 7. Error kinds
| Kind | Meaning |
|---|---|
| `empty` | blank or whitespace-only line |
| `unknown-keyword` | first token is not a recognised keyword |
| `missing-field` | too few `|`-separated fields for the form |
| `extra-field` | too many fields for the form |
| `empty-field` | a required field is empty after trimming |
| `malformed-argument` | an action argument is not `name=value` with a non-empty name and value |
| `duplicate-argument` | the same argument name appears twice |
| `invalid-tool-id` | tool id is empty or fails the pattern in §4.4 |
Every error carries a `kind` and a human-readable message. The parser reports the first error per line; whether it continues to later lines is a parser choice, not a language rule.
## 8. Examples
| Input | Outcome |
|---|---|
| `fact water \| boils_at \| 100C` | Fact{water, boils_at, 100C} |
| `fact water \| temperature \| room temperature` | Fact{water, temperature, "room temperature"} |
| `goal Summarize the file. \| A summary exists.` | Goal{...} |
| `constraint Do not change other sections.` | Constraint{...} |
| `action calculator \| left=2 \| right=3` | Action{calculator, [left:Integer=2, right:Integer=3]} |
| `action clock` | Action{clock, []} |
| `` (blank) | error `empty` |
| `sing a song` | error `unknown-keyword` |
| `fact water \| boils_at` | error `missing-field` |
| `fact water \| boils_at \| 100C \| extra` | error `extra-field` |
| `fact \| boils_at \| 100C` | error `empty-field` |
| `goal Say hello` | error `missing-field` |
| `action calculator \| left 2` | error `malformed-argument` |
| `action calculator \| left=2 \| left=3` | error `duplicate-argument` |
| `Fact water \| boils_at \| 100C` | error `unknown-keyword` (keywords are case-sensitive) |
## 9. Ambiguity policy
The language is designed to have **no valid ambiguous inputs**. Where a natural reading might permit two interpretations, the input is rejected rather than resolved by guesswork. The canonical case:
- `goal Say hello` could be read as "goal with description only" or "goal whose completion equals its description." Both are inventions. It is rejected as `missing-field`, and the caller must supply a completion condition explicitly.
This mirrors the product rule: when interpretation is materially ambiguous, ask or reject — never silently invent (PRD P4; ARCHITECTURE.md §4).
## 10. Corpus and versioning
The language is versioned independently of the knowledge-package schema. This document describes `1.0`.
A versioned test corpus accompanies the specification at `tests/corpus/controlled/v1.json`. Each entry has:
- `id` — stable identifier,
- `input` — the raw statement,
- `expect` — either `{ "statement": {...} }` or `{ "error": { "kind": "..." } }`,
- `notes` — optional rationale.
The corpus is the source of truth for M4.2's parser tests. Expected results are not edited to make regressions disappear.
## 11. Out of scope for v1
- variables, quantifiers, or pattern matching in statements;
- negation, conjunction, or conditionals;
- a literal `|` inside a field (no escaping mechanism);
- comments;
- multiple statements per line, or a statement terminator other than newline;
- numbers with exponents, thousands separators, or locale-specific formats;
- any mapping from parsed statements to domain objects;
- case-insensitive keywords.
Each deferred item is recorded so it is not silently assumed.