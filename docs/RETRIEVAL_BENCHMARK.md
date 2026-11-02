# Retrieval Benchmark
**Status:** v1.2 (M9.1 baseline + M9.2 post-change + M9.3 evaluation). Section 5 records the pre-M9.2 baseline; section 7
records the post-M9.2 position; section 9 records the M9.3 evaluation. Section 4 describes the engine before M9.2 was added.
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
- The original v1 benchmark and M9.2 lexical test path use no network, embeddings, or model and run offline against the example package. M9.3 uses a separate evaluation-only model, documented in section 9.
## 9. M9.3 frozen-embedding evaluation (2026-11-02)

### Outcome

The frozen embedding candidate passed the pre-declared quantitative thresholds against the real C#
M9.2 lexical baseline on documentation paraphrases with low lexical overlap. Exact controls did not
regress and both overall benchmark groups improved. The result justifies **a separate integration
proposal only**; it does not approve implementation or production adoption. The evaluation adds no
production provider, dependency, or behavior change. M9.2 lexical retrieval remains the only
production path and the current default/opt-out behavior.

### Reproducibility and scope

- CI: [M9.3 evaluation run 41343077786](https://github.com/Vraxo/Lattice/actions/runs/41343077786)
  on `bf36d0c` passed all evaluation steps. [Build/test run 41343077722](https://github.com/Vraxo/Lattice/actions/runs/41343077722)
  also passed.
- Artifact `m9-3-evaluation`: 20,942 bytes, digest
  `sha256:41b22c0af4ed7163f18dec6afb5aa23e8166876587c8d78b477b36d592406728`; includes per-query
  JSON, raw and guarded rankings, semantic self-test, hardware and package diagnostics.
- Fixture: 24 human-reviewed queries / 8 concepts, SHA-256
  `fa751c6e3b049938e4895a489d7b6c91b857e5c4ebc9e4135cb2c438522d12e6`. The controlled pilot
  assigns one relevant target per query; it is not representative of arbitrary documentation search.
- The actual C# M9.2 tokenizer/scorer independently selected the low-overlap subset at token overlap
  ≤ 0.25: 21 queries across all 8 targets. Each overlap ratio was cross-checked against the
  `token-overlap` scoring factor. The low-overlap lexical denominator below is computed separately.
- Both methods ranked the same fixture/package concepts. Embedding document text was preferred name,
  declared aliases, and definition. Primary embedding ranking used an exact id/name/alias guard before
  cosine ranking; raw cosine rankings are retained in the artifact but are not the adoption policy.
- Candidate: frozen `sentence-transformers/all-MiniLM-L6-v2`, Qdrant ONNX export; 384 dimensions,
  256-token truncation, FastEmbed v0.8.1-compatible tokenization, attention-mask mean pooling and L2
  normalization. No training, fine-tuning, generative use, or tuning against evaluation examples.
- Reproduction commands, model checksums and pinned dependency/wheel hashes are in
  `tests/evaluation/m9_3/README.md`, `model-manifest.json`, `requirements.lock`, `dependencies.json`,
  and `THIRD_PARTY_NOTICES.md`. Provisioning is explicit and inference is offline after provisioning.
  The user verified the model payload against official Qdrant artifact checksums; the pinned mirror
  commit history was not independently authenticated.

### Recall@5 and MRR@5

Deltas are guarded embedding minus the actual C# lexical baseline.

| Group | Queries | Lexical Recall@5 | Embedding Recall@5 | Δ Recall@5 | Lexical MRR@5 | Embedding MRR@5 | Δ MRR@5 |
|---|---:|---:|---:|---:|---:|---:|---:|
| Low-overlap documentation subset | 21 | 15/21 = 0.7143 | 21/21 = 1.0000 | +0.2857 (+28.57 pp) | 0.3905 | 0.7968 | +0.4063 |
| Full paraphrase fixture | 24 | 18/24 = 0.7500 | 24/24 = 1.0000 | +0.2500 (+25.00 pp) | 0.4667 | 0.8222 | +0.3556 |
| Existing benchmark | 8 | 7/8 = 0.8750 | 8/8 = 1.0000 | +0.1250 (+12.50 pp) | 0.8750 | 0.9375 | +0.0625 |
| Existing exact-symbol + alias controls | 4 | 4/4 = 1.0000 | 4/4 = 1.0000 | 0.0000 | 1.0000 | 1.0000 | 0.0000 |
| Generated id/name/alias controls | 38 | 38/38 = 1.0000 | 38/38 = 1.0000 | 0.0000 | 1.0000 | 1.0000 | 0.0000 |

Primary thresholds were ≥ +0.10 Recall@5 and ≥ +0.05 MRR@5 on the low-overlap subset, no exact-match
regression, and no overall regression. Observed gains (+0.2857 Recall@5, +0.4063 MRR@5) exceed both
thresholds; exact controls are unchanged; the full paraphrase and existing benchmark both improve.

The legacy benchmark's two `related-concept` rows had guarded embedding Recall@5 1.0 / MRR@5 0.75.
This is an incidental result only; it does not establish relationship semantics, support graph
traversal, or alter the documentation-paraphrase scope. Relationship claims require explicit facts /
relations and separately authorized traversal.

### Operational cost (GitHub Actions `ubuntu-latest`, CPU)

| Measure | Result |
|---|---:|
| Model load | 0.273 s |
| Embed 10 documents | 0.084 s |
| Total model files / ONNX file | 91,345,404 B (~87.1 MiB) / 90,387,630 B (~86.2 MiB) |
| RSS before → after document embedding | 182,996,992 B → 186,978,304 B (+3,981,312 B, ~3.8 MiB) |
| Process peak RSS | 210,845,696 B (~201.0 MiB) |
| Warm single-query embedding latency | 640 samples: median 5.38 ms, p95 7.01 ms, max 9.76 ms |
| Runtime | ONNX Runtime CPU; 1 intra-op / 1 inter-op thread |

Latency is for the warmed single-query embedding path, not end-to-end application or vector-store
latency. RSS-before is after model initialization/self-test, so it is not a model-vs-no-model delta.
The exact CPU model and available RAM were not included in the pasted summary. The measured disk,
load, and latency costs appear plausible for an optional CPU-backed feature, but no constrained-device
or end-to-end production measurement was made.

### Licensing, reproducibility, inspectability, opt-out

- **Licensing:** declared model license Apache-2.0; package licenses/notices were inventoried. The
  installed NumPy distribution's bundled OpenBLAS/LAPACK, GCC Fortran runtime, quadmath, random, and
  masked-array notices were inspected, as were ONNX Runtime third-party notices. This is a declared
  license inventory, not legal review.
- **Reproducibility:** pinned versions and wheel hashes, model-file SHA-256 checks, frozen fixture
  hash, explicit provisioning, and offline inference all passed CI. Qualification: mirror history is
  unauthenticated even though the payload was checked against official Qdrant checksums.
- **Inspectability:** tokenization/truncation/padding, exact-guard policy, pooling, normalization, and
  ranking are explicit; the harness stores per-query diagnostics and runs semantic/determinism checks.
- **Opt-out:** there is no production embedding path or mandatory dependency; lexical retrieval is
  unchanged and remains the only production behavior. A provider-specific disable/fallback switch is
  therefore not applicable or tested in this evaluation. Any later integration proposal must include
  and test explicit opt-out/fallback, retain source attribution, and keep ranking advisory only—not an
  authority over facts, permissions, policies, relationships, or action selection.

**M9.3 evaluation gate: PASS.** The evidence supports only a separate integration proposal, not
production implementation. The pilot is small, curated, English-only, and single-target; it makes no
statistical-significance or broad-retrieval claim. No graph traversal, production dependency/provider,
training, fine-tuning, or generative use is authorized by this result.
