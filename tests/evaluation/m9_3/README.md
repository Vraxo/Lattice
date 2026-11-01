# M9.3 retrieval-provider evaluation

This directory is **evaluation-only**. It is outside `Lattice.slnx`, outside the normal test execution path, and outside production projects. It does not add a production dependency, provider, or graph traversal. The existing M9.2 lexical implementation remains the baseline and opt-out.

## Question

Can one frozen pretrained embedding system retrieve relevant documentation when user wording has little lexical overlap, without regressing exact identifier/name/alias retrieval or the existing benchmark?

This is a controlled pilot. Each query has one assigned relevant target so Recall@5 and MRR@5 are unambiguous; that is a corpus design choice, not a claim about documentation retrieval generally.

## Frozen inputs

- Paraphrase fixture: `tests/corpus/retrieval/paraphrases/v1.json`
  - 24 queries, 8 documentation concepts.
  - `reviewStatus: human-reviewed`.
  - SHA-256: `fa751c6e3b049938e4895a489d7b6c91b857e5c4ebc9e4135cb2c438522d12e6`.
  - The low-overlap subset is computed by the real C# M9.2 tokenizer/scorer, not by the Python evaluation.
- Existing benchmark: `tests/corpus/retrieval/v1.json` and `knowledge/example.physics/package.json`.
- Model manifest: `model-manifest.json`.
- Dependency lock: `requirements.lock`; machine-readable manifest: `dependencies.json`; notices: `THIRD_PARTY_NOTICES.md`.

## Reproducible setup

The model is deliberately kept out of ordinary Git history under ignored `artifacts/`.

```bash
python3 -m venv artifacts/evaluation/m9_3/venv
artifacts/evaluation/m9_3/venv/bin/python -m pip install --disable-pip-version-check --only-binary=:all: --require-hashes -r tests/evaluation/m9_3/requirements.lock
python3 tests/evaluation/m9_3/provision.py
```

`provision.py` downloads the pinned mirror content and verifies every SHA-256 against both `model-manifest.json` and the mirror's `SHA256SUMS`. After provisioning, evaluation performs no network access.

## CI execution

`.github/workflows/m9_3-evaluation.yml` runs the isolated evaluation on GitHub Actions (`ubuntu-latest`). It runs the real C# baseline harness, installs the hash-locked dependencies, provisions the checksum-verified model, runs the semantic self-test and the embedding comparison, and uploads `baseline.json`, `self-test.json`, and `embedding-results.json` as the `m9-3-evaluation` artifact. It triggers on manual dispatch, on pull requests touching evaluation inputs, and on pushes to `arena/effad8bb-lattice` that touch evaluation files.

The full solution build/test gate stays in `ci.yml` on `windows-latest`. This workflow deliberately does not re-run `Lattice.slnx` tests, so a platform-specific suite difference cannot mask or block the evaluation measurements. The harness builds `Lattice.Core` transitively through its `ProjectReference`.

## Real C# lexical baseline

The baseline must come from the actual M9.2 C# implementation. The harness calls `KnowledgeLookup.Search` and uses reflection only to observe internal `LexicalScorer` tokenizer/factor diagnostics.

```bash
dotnet run --project tests/evaluation/m9_3/baseline -c Release -- . artifacts/evaluation/m9_3/baseline.json
```

The harness writes `artifacts/evaluation/m9_3/baseline.json`, including the independently computed low-overlap ids, per-query token counts/overlap ratios, paraphrase metrics, existing-benchmark metrics, exact-match controls, and lexical operational timing.

## Embedding comparison

```bash
artifacts/evaluation/m9_3/venv/bin/python tests/evaluation/m9_3/evaluate.py --self-test --output artifacts/evaluation/m9_3/self-test.json
artifacts/evaluation/m9_3/venv/bin/python tests/evaluation/m9_3/evaluate.py --baseline artifacts/evaluation/m9_3/baseline.json --output artifacts/evaluation/m9_3/embedding-results.json
```

`evaluate.py` refuses to run the comparison unless the baseline was produced by the C# harness and its fixture hash matches the frozen fixture.

### Fixed evaluation policy

- Model: `sentence-transformers/all-MiniLM-L6-v2`, Qdrant ONNX export, frozen, no training/fine-tuning/generative use.
- Semantics reference: FastEmbed `v0.8.1`, commit `8de28b8f2d4525167c7ebe988aa06b740a59e0c0`.
- Tokenization: 256-token truncation, batch-longest padding, special tokens from the export, zero `token_type_ids`.
- Post-processing: attention-mask mean pooling, then L2 normalization with eps `1e-12`.
- Document text: preferred name + declared aliases + definition. Identifiers are handled by the exact guard, not cosine.
- Primary candidate policy: exact id/preferred/alias matches first, then cosine ranking of remaining documents. Raw unguarded cosine behavior is reported separately.
- Runtime: ONNX Runtime CPU, single intra-op and inter-op thread, offline environment flags.
- No provider/scoring tuning against evaluation examples.

## Adoption thresholds

On the primary low-overlap subset:

- Recall@5 improvement ≥ +10 percentage points.
- MRR@5 improvement ≥ +0.05 absolute.
- No exact-match regression.
- No overall benchmark regression.
- Acceptable overhead, licensing, reproducibility, and inspectability.

A quantitative pass does **not** authorize production integration. If adoption is justified, the next step is a separate integration proposal only. If not justified, record the measurements, keep lexical retrieval, and introduce no dependency/provider.
