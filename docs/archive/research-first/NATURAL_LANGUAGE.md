# Natural-Language Interpretation — Bounded Patterns
**Status:** v1.1 draft (M4.4)
**Pattern version:** `1.0`
## 1. Purpose and honesty statement
This document describes the **natural-language path**: how an ordinary user request is mapped, if possible, onto a small set of known intents.
It is deliberately tiny. It does **not** understand arbitrary English. It matches a fixed list of phrasings, and when nothing matches it says so. The controlled path (`docs/CONTROLLED_LANGUAGE.md`) remains the unambiguous route for statements that matter.
The design rule: **unknown or uncertain input is never assigned a confident intent.**
## 2. Intents
| Intent | Meaning |
|---|---|
| `question` | The user asks for a fact or an answer. |
| `explanation` | The user asks why or how something works. |
| `find-symbol` | The user wants a named symbol located. |
| `inspect-path` | The user wants a file or path examined. |
| `unknown` | Not a confident intent. Used when nothing matched, or when matches tied. |
`unknown` is a real, expected outcome, not a failure of the system.
## 3. Outcome kinds
Interpretation returns exactly one of four kinds — typed uncertainty, never a numeric confidence:
| Kind | Meaning |
|---|---|
| `Matched` | Exactly one pattern was the most specific. Its intent and slot are reported. |
| `MissingValue` | The input was a slot pattern with its value omitted, and no other pattern matched. The intent and the missing slot name are reported. |
| `Ambiguous` | Two or more patterns tied at the top specificity. Competing candidates are reported; intent is `unknown`. |
| `Unknown` | No pattern matched, or the input was empty. A diagnostic explains why. |
## 4. Pattern format
Patterns are data, not code. They live in `config/nlp/patterns.json`.
```json
{
  "patternVersion": "1.0",
  "patterns": [
    { "id": "question.what-is", "intent": "question", "template": "what is {topic}" }
  ]
}
```
| Field | Required | Notes |
|---|---|---|
| `patternVersion` | yes | `MAJOR.MINOR`. Major `1` is supported; unknown major rejected. |
| `patterns` | yes | array of pattern objects |
| `patterns[].id` | yes | unique within the file |
| `patterns[].intent` | yes | one of `question`, `explanation`, `find-symbol`, `inspect-path` |
| `patterns[].template` | yes | see §5 |
Unknown fields are rejected (fail closed), matching the knowledge-package policy.
## 5. Template rules
A template is whitespace-separated tokens. Each token is either a **literal** or a **slot** written `{name}`.
- A template must contain **at least one literal**.
- A template may contain **at most one slot**, and the slot must be the **final** token.
- A slot name must start with a letter.
- Literal comparison is **case-insensitive**.
Valid: `what is {topic}`, `find symbol {symbol}`, `what is`.
Invalid: `{topic}` (no literal), `{a} is {b}` (two slots), `is {a} true` (slot not final).
## 6. Matching algorithm
1. Tokenize the input on whitespace; empty tokens are dropped.
2. A pattern matches when its literal tokens lead the input, in order, case-insensitively.
   - With a slot: the slot absorbs **all** remaining tokens, and there must be at least one. `what is` alone does not match `what is {topic}`.
   - Without a slot: the input must have **exactly** the literal tokens. `explain` does not match a pattern of `explain` plus extra words.
3. The **specificity** of a match is its number of literal tokens. More literals means more specific.
4. The highest specificity wins. If exactly one match has it, the result is `Matched`.
5. If two or more matches tie at the highest specificity, the result is `Ambiguous` — even if the tied patterns share an intent. The engine does not break the tie by guessing.
6. If no pattern matched with a value, check for **missing values**: a pattern whose literals exactly equal the input and whose slot was left empty. This is only consulted when no ordinary match exists, so it never overrides a real match.
   - Exactly one such pattern → `MissingValue`, reporting that pattern's intent and missing slot name.
   - Two or more → `Ambiguous`.
7. Otherwise the result is `Unknown`.
Because a slot consumes the remainder, the reported slot value is the remaining tokens joined by single spaces. Runs of whitespace are therefore normalized in the slot value; the original text is preserved separately.
## 7. Clarification
`ClarificationGenerator.TryCreate` turns a non-`Matched` interpretation into a single concise question, or returns `null` for `Matched`:
| Interpretation | Clarification | Example question |
|---|---|---|
| `Matched` | none | — |
| `MissingValue` | `MissingValue` | `What topic?` |
| `Ambiguous` | `AmbiguousIntent` | `Did you mean an explanation or a file inspection?` |
| `Unknown` | `UnknownIntent` | `I did not understand that request. Could you rephrase it?` |
Rules:
- An ambiguous set whose candidates share one intent cannot be disambiguated by naming intents, so it falls back to a generic *"be more specific"* question.
- Generation is deterministic: the same interpretation yields the same question.
- The generator **asks; it does not act.** Choosing a next action belongs to the controller (M5).
## 8. Worked examples
| Input | Outcome | Why |
|---|---|---|
| `what is the capital of France` | `Matched`, `question`, `topic=the capital of France` | one pattern at specificity 2 |
| `why is the sky blue` | `Matched`, `explanation`, `subject=is the sky blue` | `why {subject}` at specificity 1 |
| `explain this file` | `Matched`, `explanation`, `topic=this file` | paraphrase of the same intent via a different pattern |
| `find symbol Calculator` | `Matched`, `find-symbol`, `symbol=Calculator` | `find symbol {symbol}` (specificity 2) beats `find {symbol}` (specificity 1) |
| `what is` | `MissingValue`, `question`, slot `topic` | the slot was left empty and nothing else matched |
| `show README.md` | `Ambiguous` | `show {target}` is defined twice, as `inspect-path` and as `explanation` |
| `hello there` | `Unknown` | no pattern's literals lead the input |
| `` (empty) | `Unknown` | empty input |
## 9. What this deliberately does not do
- No stemming, lemmatisation, or synonym expansion.
- No fuzzy or partial matching beyond the prefix rule in §6.
- No multi-slot patterns, no optional tokens, no negation.
- No numeric confidence scores.
- No learning. Adding coverage means adding data.
- No routing decision. This layer reports intent and asks questions; choosing an action is a later concern.
Every limitation is recorded so it is not silently assumed away.