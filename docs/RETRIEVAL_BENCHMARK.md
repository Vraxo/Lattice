# Retrieval Benchmark
**Status:** v1.1 (M9.1 baseline + M9.2 post-change). Section 5 records the pre-M9.2 baseline; section 7
records the post-M9.2 position. Section 4 describes the engine before M9.2 was added.
## 1. Purpose
The roadmap (M9.1) requires a small, human-reviewed retrieval benchmark and a recorded baseline
before adding lexical, fuzzy, or embedding-based retrieval. The point is to measure a gap rather than
assume one. This document records the query set, the metric definitions, and the measured baseline.
The corpus is the source of truth: `tests/corpus/retrieval/v1.json`. Expected results are not edited
to make a regression disappear.
## 2. Query classes
| Class | What a correct retriever returns |
|---|---|
| `exact-symbol` | The concept whose id exactly equals the query. |
| `alias` | The concept that declares the query as an alias (case-insensitive). |
| `related-concept` | A concept connected to the query through a fact or relation. |
| `documentation` | A concept whose definition or documentation text matches the query. |
## 3. Metric definitions
Micro-averaged within each class:
- `truePositives` = retrieved ids that appear in the entry's expected set.
- `precision` = `truePositives / retrieved`; if nothing was retrieved, `1.0` when nothing was
  expected and `0.0` otherwise.
- `recall` = `truePositives / expected`; `1.0` when nothing was expected.
Micro-averaging (summing across entries before dividing) keeps a class with more entries from being
dominated by a class with fewer.
## 4. What the engine retrieved at baseline (before M9.2)
The only retrieval operations are `KnowledgeLookup.FindById` (exact id) and `FindByAlias`
(case-insensitive exact alias). There is no relation traversal and no full-text or documentation
search. The benchmark adapter therefore serves `exact-symbol` and `alias` queries and returns nothing
for the other two classes. That is recorded as a real gap, not hidden.
## 5. Baseline (M9.1)
| Class | Precision | Recall |
|---|---|---|
| `exact-symbol` | 1.000 | 1.000 |
| `alias` | 1.000 | 1.000 |
| `related-concept` | 0.000 | 0.000 |
| `documentation` | 0.000 | 0.000 |
Interpretation:
- The engine is **exact and correct** where it has an operation: every exact-id and alias query in the
  benchmark is served with no false positives.
- The engine **cannot serve** related-concept or documentation queries at all. This is the measured
  gap that M9.2 (configurable lexical/fuzzy retrieval) and, if justified, M9.3 (frozen embeddings)
  exist to close.
## 6. How this is enforced
`RetrievalBenchmarkTests` asserts, per class, that the measured precision and recall equal the
recorded baseline, and asserts the two unserved classes stay at recall 0 until retrieval changes. It
also checks that every expected id exists in the package and that the baseline covers every query
class in the corpus.
Adding retrieval sophistication **must** update the corpus and the baseline deliberately, in the same
commit, with the new numbers reported. A test failure here is the intended signal that measured
behaviour changed.
## 7. M9.2 post-change position
M9.2 adds ranked lexical retrieval (`KnowledgeLookup.Search`) over concept id, preferred name,
aliases, and definition text. It deliberately does **not** add fact/relation traversal.
Derived from the corpus (asserted by tests as inequalities, not hard-coded numbers, because they were
not measured by a run in this session):
| Class | Baseline recall | Post-M9.2 | Note |
|---|---|---|---|
| `exact-symbol` | 1.000 | 1.000 | No regression; asserted. |
| `alias` | 1.000 | 1.000 | No regression; asserted. |
| `documentation` | 0.000 | > 0 | Both documentation entries are served by definition-text matching. |
| `related-concept` | 0.000 | 0.000 | Unchanged with this corpus: neither query's words appear in concept text. Lexical retrieval would only serve this class incidentally, and that would still not be traversal. |
The proof that graph traversal was **not** added is a dedicated test: a query connected to a concept
only through a fact (`boiling point` → `boils_at`) retrieves nothing. The `related-concept` recall
stays at 0.000 for this corpus; serving that class requires fact/relation traversal, which is a
separately scoped follow-up.
Ranking quality is measured with MRR@5 over the ranked interface. Precision@5 is not asserted
because the example package yields at most one relevant concept per query; add it if the corpus grows
entries with multiple relevant results.
Scores are ranking values, not probabilities. The total score is the sum of factor contributions
(`weight × rawScore`), so every ranking is explainable from its `RetrievalFactor` list. Ties break by
concept id ordinal for determinism.
## 8. Non-goals
- No statistical significance claims; the set is tiny and hand-authored.
- No precision@k yet: the example package yields at most one relevant concept per query, so
  precision@5 would not be informative. Add it when the corpus grows multi-relevant entries. (MRR@5
  was added in M9.2; see section 7.)
- No network, no embeddings, no model. The benchmark runs offline against the example package.