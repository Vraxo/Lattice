#!/usr/bin/env python3
"""M9.3 evaluation-only frozen embedding comparison.

Requires a baseline JSON produced by the real C# harness in baseline/. This script
never tunes against the evaluation fixture. It verifies the frozen fixture hash,
loads the checksum-verified model artifact, replicates FastEmbed v0.8.1 semantics
for all-MiniLM-L6-v2, and reports raw and exact-guarded embedding rankings beside
the lexical baseline. No network access is performed.
"""

from __future__ import annotations

import argparse
import hashlib
import importlib.metadata
import json
import os
import platform
import sys
import time
from pathlib import Path
from typing import Any

# Offline flags must be set before importing tokenizers/onnxruntime/huggingface code.
os.environ.setdefault("HF_HUB_OFFLINE", "1")
os.environ.setdefault("TRANSFORMERS_OFFLINE", "1")
os.environ.setdefault("HF_HUB_DISABLE_TELEMETRY", "1")
os.environ.setdefault("TOKENIZERS_PARALLELISM", "false")

import numpy as np
import onnxruntime as ort
from tokenizers import AddedToken, Tokenizer

try:  # POSIX only; peak RSS is optional evidence.
    import resource
except ImportError:  # pragma: no cover - Windows
    resource = None  # type: ignore[assignment]

HERE = Path(__file__).resolve().parent
REPO_ROOT = HERE.parents[2]
DEFAULT_MODEL_DIR = REPO_ROOT / "artifacts" / "evaluation" / "m9_3" / "model"
DEFAULT_BASELINE = REPO_ROOT / "artifacts" / "evaluation" / "m9_3" / "baseline.json"
DEFAULT_OUTPUT = REPO_ROOT / "artifacts" / "evaluation" / "m9_3" / "embedding-results.json"
FIXTURE_PATH = REPO_ROOT / "tests" / "corpus" / "retrieval" / "paraphrases" / "v1.json"
CORPUS_PATH = REPO_ROOT / "tests" / "corpus" / "retrieval" / "v1.json"
PHYSICS_PATH = REPO_ROOT / "knowledge" / "example.physics" / "package.json"
MANIFEST_PATH = HERE / "model-manifest.json"
LOW_OVERLAP_THRESHOLD = 0.25
RECALL_DELTA_THRESHOLD = 0.10
MRR_DELTA_THRESHOLD = 0.05


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def verify_model(model_dir: Path, manifest: dict[str, Any]) -> dict[str, Any]:
    verified = {}
    entries = list(manifest["files"]) + [manifest["licenseFile"]]
    for entry in entries:
        path = model_dir / entry["path"]
        if not path.exists():
            raise SystemExit(f"Missing model artifact {path}; run provision.py first.")
        actual = sha256_file(path)
        if actual != entry["sha256"]:
            raise SystemExit(f"SHA-256 mismatch for {path}: expected {entry['sha256']}, got {actual}")
        verified[entry["path"]] = {"sha256": actual, "bytes": path.stat().st_size}
    return verified


def valid_context(value: Any) -> int | None:
    if isinstance(value, bool) or not isinstance(value, int):
        return None
    if not 0 < value <= sys.maxsize:
        return None
    return value


def resolve_max_context(tokenizer_config: dict[str, Any]) -> int:
    candidates = [
        context
        for context in (
            valid_context(tokenizer_config.get("model_max_length")),
            valid_context(tokenizer_config.get("max_length")),
        )
        if context is not None
    ]
    if not candidates:
        raise SystemExit("Could not determine tokenizer max context length.")
    return min(candidates)


def iter_special_tokens(tokens_map: dict[str, Any]):
    for value in tokens_map.values():
        if isinstance(value, list):
            yield from value
        else:
            yield value


def load_tokenizer(model_dir: Path) -> tuple[Tokenizer, int]:
    """Replicate fastembed.common.preprocessor_utils.load_tokenizer at v0.8.1."""
    tokenizer_config = read_json(model_dir / "tokenizer_config.json")
    config = read_json(model_dir / "config.json") if (model_dir / "config.json").exists() else {}
    max_context = resolve_max_context(tokenizer_config)
    tokenizer = Tokenizer.from_file(str(model_dir / "tokenizer.json"))
    tokenizer.enable_truncation(max_length=max_context)

    tokens_map_path = model_dir / "special_tokens_map.json"
    tokens_map = read_json(tokens_map_path) if tokens_map_path.exists() else {}
    for token in iter_special_tokens(tokens_map):
        if isinstance(token, str):
            tokenizer.add_special_tokens([token])
        elif isinstance(token, dict):
            tokenizer.add_special_tokens([AddedToken(**token)])

    padding = tokenizer.padding or {}
    pad_token = padding.get("pad_token") or tokenizer_config.get("pad_token")
    if pad_token is None:
        raise SystemExit("Could not find a pad token.")
    pad_id = padding.get("pad_id", config.get("pad_token_id"))
    if pad_id is None:
        pad_id = tokenizer.token_to_id(pad_token)
    if pad_id is None:
        raise SystemExit(f"Could not resolve pad token id for {pad_token!r}.")
    tokenizer.enable_padding(
        direction=padding.get("direction", "right"),
        pad_id=pad_id,
        pad_type_id=padding.get("pad_type_id", 0),
        pad_token=pad_token,
        pad_to_multiple_of=padding.get("pad_to_multiple_of"),
        length=None,
    )
    return tokenizer, max_context


def mean_pooling(model_output: np.ndarray, attention_mask: np.ndarray) -> np.ndarray:
    """Replicate fastembed.common.utils.mean_pooling at v0.8.1."""
    input_mask_expanded = np.expand_dims(attention_mask, axis=-1).astype(np.int64)
    input_mask_expanded = np.tile(input_mask_expanded, (1, 1, model_output.shape[-1]))
    sum_embeddings = np.sum(model_output * input_mask_expanded, axis=1)
    sum_mask = np.sum(input_mask_expanded, axis=1)
    return sum_embeddings / np.maximum(sum_mask, 1e-9)


def normalize(input_array: np.ndarray) -> np.ndarray:
    """Replicate fastembed.common.utils.normalize at v0.8.1 (L2, eps=1e-12)."""
    norm = np.linalg.norm(input_array, ord=2, axis=1, keepdims=True)
    norm = np.maximum(norm, 1e-12)
    return input_array / norm


class FrozenEmbedder:
    def __init__(self, model_dir: Path):
        self.manifest = read_json(MANIFEST_PATH)
        self.verified_files = verify_model(model_dir, self.manifest)
        self.model_dir = model_dir
        self.tokenizer, self.max_context = load_tokenizer(model_dir)
        options = ort.SessionOptions()
        options.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL
        options.intra_op_num_threads = 1
        options.inter_op_num_threads = 1
        self.session = ort.InferenceSession(
            str(model_dir / "model.onnx"),
            sess_options=options,
            providers=["CPUExecutionProvider"],
        )
        self.input_names = {node.name for node in self.session.get_inputs()}
        self.output_names = [node.name for node in self.session.get_outputs()]
        self.dimension = int(self.session.get_outputs()[0].shape[-1])

    def encode(self, texts: list[str]) -> tuple[np.ndarray, dict[str, Any]]:
        encoded = self.tokenizer.encode_batch(texts)
        input_ids = np.array([encoding.ids for encoding in encoded], dtype=np.int64)
        attention_mask = np.array([encoding.attention_mask for encoding in encoded], dtype=np.int64)
        onnx_input: dict[str, np.ndarray] = {"input_ids": input_ids}
        if "attention_mask" in self.input_names:
            onnx_input["attention_mask"] = attention_mask
        if "token_type_ids" in self.input_names:
            onnx_input["token_type_ids"] = np.zeros_like(input_ids, dtype=np.int64)
        output = self.session.run(None, onnx_input)[0]
        pooled = mean_pooling(output, attention_mask)
        embeddings = normalize(pooled)
        diagnostics = {
            "batchSize": len(texts),
            "sequenceLength": int(input_ids.shape[1]),
            "maxContext": self.max_context,
            "tokenTypeIdsAllZero": bool(np.all(onnx_input.get("token_type_ids", np.zeros(1)) == 0)),
            "attentionMaskShape": list(attention_mask.shape),
        }
        return embeddings, diagnostics

    def embed_one(self, text: str) -> np.ndarray:
        embeddings, _ = self.encode([text])
        return embeddings[0]


def concept_document_text(concept: dict[str, Any]) -> str:
    """Fixed document representation declared before measurement.

    The semantic document text contains the preferred name, declared aliases, and
    definition. Identifiers are handled by the exact-match guard, not by cosine.
    """
    parts = [concept["preferred"]]
    aliases = concept.get("aliases") or []
    if aliases:
        parts.append(", ".join(aliases))
    definition = concept.get("definition")
    if definition:
        parts.append(definition)
    return "\n".join(parts)


def exact_matches(query: str, concepts: list[dict[str, Any]]) -> list[dict[str, Any]]:
    trimmed = query.strip()
    matches = []
    for concept in concepts:
        candidates = [concept["id"], concept["preferred"], *(concept.get("aliases") or [])]
        if any(candidate.casefold() == trimmed.casefold() for candidate in candidates):
            matches.append(concept)
    return sorted(matches, key=lambda concept: concept["id"])


def rank_concepts(
    query_embedding: np.ndarray,
    concepts: list[dict[str, Any]],
    document_embeddings: np.ndarray,
    guarded: bool,
    query: str,
) -> list[dict[str, Any]]:
    scores = document_embeddings @ query_embedding
    order = sorted(range(len(concepts)), key=lambda index: (-float(scores[index]), concepts[index]["id"]))
    ranked = [
        {"id": concepts[index]["id"], "score": float(scores[index])}
        for index in order
    ]
    if not guarded:
        return ranked[:5]
    exact = exact_matches(query, concepts)
    exact_ids = {concept["id"] for concept in exact}
    guarded_ranked = [{"id": concept["id"], "score": None, "exact": True} for concept in exact]
    guarded_ranked.extend(row for row in ranked if row["id"] not in exact_ids)
    return guarded_ranked[:5]


def metrics(rows: list[dict[str, Any]]) -> dict[str, Any]:
    count = len(rows)
    hits = sum(1 for row in rows if row["hitAt5"])
    mrr = sum(row["reciprocalRank"] for row in rows) / count if count else 0.0
    return {
        "queryCount": count,
        "hitsAt5": hits,
        "recallAt5": hits / count if count else 0.0,
        "mrrAt5": mrr,
    }


def evaluate_rows(
    entries: list[dict[str, Any]],
    concepts: list[dict[str, Any]],
    embedder: FrozenEmbedder,
    document_embeddings: np.ndarray,
    guarded: bool,
) -> list[dict[str, Any]]:
    rows = []
    for entry in entries:
        expected_ids = entry["expectedIds"]
        query_embedding = embedder.embed_one(entry["query"])
        ranked = rank_concepts(query_embedding, concepts, document_embeddings, guarded, entry["query"])
        rank = 0
        for index, candidate in enumerate(ranked, start=1):
            if candidate["id"] in expected_ids:
                rank = index
                break
        rows.append(
            {
                "id": entry["id"],
                "query": entry["query"],
                "expectedIds": expected_ids,
                "top5": ranked,
                "rank": rank or None,
                "reciprocalRank": 1.0 / rank if rank else 0.0,
                "hitAt5": rank > 0,
                "top1Exact": bool(ranked and ranked[0]["id"] in expected_ids),
            }
        )
    return rows


def percentile(values: list[float], quantile: float) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    index = (len(ordered) - 1) * quantile
    lower = int(index)
    upper = min(lower + 1, len(ordered) - 1)
    return ordered[lower] + (ordered[upper] - ordered[lower]) * (index - lower)


def rss_bytes() -> int | None:
    status = Path("/proc/self/status")
    if status.exists():
        for line in status.read_text().splitlines():
            if line.startswith("VmRSS:"):
                return int(line.split()[1]) * 1024
    return None


def hardware_info() -> dict[str, Any]:
    cpu_model = None
    cpuinfo = Path("/proc/cpuinfo")
    if cpuinfo.exists():
        for line in cpuinfo.read_text().splitlines():
            if line.startswith("model name"):
                cpu_model = line.split(":", 1)[1].strip()
                break
    mem_total = None
    meminfo = Path("/proc/meminfo")
    if meminfo.exists():
        for line in meminfo.read_text().splitlines():
            if line.startswith("MemTotal:"):
                mem_total = int(line.split()[1]) * 1024
                break
    return {
        "platform": platform.platform(),
        "machine": platform.machine(),
        "processor": platform.processor(),
        "cpuModel": cpu_model,
        "logicalCpuCount": os.cpu_count(),
        "memoryTotalBytes": mem_total,
        "pythonVersion": platform.python_version(),
    }


def package_versions() -> dict[str, str]:
    names = ["numpy", "onnxruntime", "tokenizers"]
    return {name: importlib.metadata.version(name) for name in names}


def self_test(embedder: FrozenEmbedder) -> dict[str, Any]:
    long_text = "word " * 400
    encoded = embedder.tokenizer.encode(long_text)
    if len(encoded.ids) != embedder.max_context:
        raise SystemExit(f"Truncation self-test failed: {len(encoded.ids)} != {embedder.max_context}")
    if encoded.ids[0] != embedder.tokenizer.token_to_id("[CLS]") or encoded.ids[-1] != embedder.tokenizer.token_to_id("[SEP]"):
        raise SystemExit("Special-token self-test failed.")
    batch = embedder.tokenizer.encode_batch(["short", long_text])
    if len(batch[0].ids) != len(batch[1].ids):
        raise SystemExit("Batch-longest padding self-test failed.")
    if sum(batch[0].attention_mask) >= sum(batch[1].attention_mask):
        raise SystemExit("Attention-mask self-test failed.")
    embeddings, diagnostics = embedder.encode(["hello world", "a longer document about software architecture"])
    norms = np.linalg.norm(embeddings, axis=1)
    if not np.allclose(norms, 1.0, atol=1e-5):
        raise SystemExit(f"Normalization self-test failed: {norms}")
    again, _ = embedder.encode(["hello world", "a longer document about software architecture"])
    max_diff = float(np.max(np.abs(embeddings - again)))
    if max_diff > 1e-6:
        raise SystemExit(f"Determinism self-test failed: {max_diff}")
    return {
        "truncationTokens": len(encoded.ids),
        "batchPaddingSequenceLength": len(batch[0].ids),
        "normalizedNorms": norms.tolist(),
        "determinismMaxAbsDiff": max_diff,
        "diagnostics": diagnostics,
        "dimension": embedder.dimension,
        "inputNames": sorted(embedder.input_names),
        "outputNames": embedder.output_names,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--model-dir", type=Path, default=DEFAULT_MODEL_DIR)
    parser.add_argument("--baseline", type=Path, default=DEFAULT_BASELINE)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--self-test", action="store_true", help="Run model/tokenizer semantic self-tests without comparing the fixture.")
    parser.add_argument("--latency-repeats", type=int, default=20)
    args = parser.parse_args()

    load_start = time.perf_counter()
    embedder = FrozenEmbedder(args.model_dir.resolve())
    load_seconds = time.perf_counter() - load_start
    tests = self_test(embedder)
    if args.self_test:
        result = {
            "mode": "self-test",
            "modelManifest": embedder.manifest["model"],
            "verifiedFiles": embedder.verified_files,
            "selfTest": tests,
            "packageVersions": package_versions(),
            "hardware": hardware_info(),
            "modelLoadSeconds": load_seconds,
        }
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(result, indent=2))
        return 0

    if not args.baseline.exists():
        raise SystemExit(
            f"Missing real C# baseline {args.baseline}. Run: dotnet run --project tests/evaluation/m9_3/baseline -c Release -- . {args.baseline}"
        )
    baseline = read_json(args.baseline)
    if baseline.get("generator") != "Lattice.M93.Baseline":
        raise SystemExit("Baseline was not produced by the real C# M9.3 harness.")
    fixture_sha256 = sha256_file(FIXTURE_PATH)
    if baseline.get("fixtureSha256") != fixture_sha256:
        raise SystemExit("Baseline fixture hash does not match the current frozen fixture.")
    fixture = read_json(FIXTURE_PATH)
    if fixture.get("reviewStatus") != "human-reviewed":
        raise SystemExit("Fixture is not human-reviewed.")
    low_overlap_ids = baseline.get("lowOverlapIds")
    if not isinstance(low_overlap_ids, list):
        raise SystemExit("Baseline does not contain the independently computed low-overlap subset.")
    fixture_entry_ids = {entry["id"] for entry in fixture["entries"]}
    if not set(low_overlap_ids) <= fixture_entry_ids:
        raise SystemExit("Baseline low-overlap ids are not a subset of the frozen fixture.")
    low_target_ids = {
        entry["expectedIds"][0]
        for entry in fixture["entries"]
        if entry["id"] in low_overlap_ids
    }
    if len(low_overlap_ids) < 20 or len(low_target_ids) < 5:
        raise SystemExit(
            f"The low-overlap subset must contain at least 20 queries and 5 targets; got {len(low_overlap_ids)} queries and {len(low_target_ids)} targets."
        )

    corpus = read_json(CORPUS_PATH)
    physics = read_json(PHYSICS_PATH)
    paraphrase_concepts = fixture["concepts"]
    physics_concepts = physics["concepts"]

    rss_before = rss_bytes()
    doc_start = time.perf_counter()
    paraphrase_texts = [concept_document_text(concept) for concept in paraphrase_concepts]
    physics_texts = [concept_document_text(concept) for concept in physics_concepts]
    paraphrase_embeddings, paraphrase_doc_diag = embedder.encode(paraphrase_texts)
    physics_embeddings, physics_doc_diag = embedder.encode(physics_texts)
    doc_seconds = time.perf_counter() - doc_start
    rss_after_docs = rss_bytes()

    paraphrase_entries = fixture["entries"]
    corpus_entries = corpus["entries"]
    exact_existing_entries = [entry for entry in corpus_entries if entry["class"] in {"exact-symbol", "alias"}]

    guarded_paraphrase = evaluate_rows(paraphrase_entries, paraphrase_concepts, embedder, paraphrase_embeddings, True)
    raw_paraphrase = evaluate_rows(paraphrase_entries, paraphrase_concepts, embedder, paraphrase_embeddings, False)
    guarded_low = [row for row in guarded_paraphrase if row["id"] in low_overlap_ids]
    raw_low = [row for row in raw_paraphrase if row["id"] in low_overlap_ids]
    guarded_corpus = evaluate_rows(corpus_entries, physics_concepts, embedder, physics_embeddings, True)
    raw_corpus = evaluate_rows(corpus_entries, physics_concepts, embedder, physics_embeddings, False)
    guarded_exact_existing = evaluate_rows(exact_existing_entries, physics_concepts, embedder, physics_embeddings, True)
    raw_exact_existing = evaluate_rows(exact_existing_entries, physics_concepts, embedder, physics_embeddings, False)

    generated_exact_rows = []
    raw_generated_exact_rows = []
    for package_concepts, package_embeddings, package_id in [
        (paraphrase_concepts, paraphrase_embeddings, fixture["packageId"]),
        (physics_concepts, physics_embeddings, physics["id"]),
    ]:
        for concept in package_concepts:
            queries = [("id", concept["id"]), ("preferred", concept["preferred"])]
            queries.extend(("alias", alias) for alias in concept.get("aliases") or [])
            for kind, query in queries:
                entry = {"id": f"{package_id}:{concept['id']}:{kind}", "query": query, "expectedIds": [concept["id"]]}
                guarded = evaluate_rows([entry], package_concepts, embedder, package_embeddings, True)[0]
                raw = evaluate_rows([entry], package_concepts, embedder, package_embeddings, False)[0]
                guarded.update({"packageId": package_id, "conceptId": concept["id"], "kind": kind})
                raw.update({"packageId": package_id, "conceptId": concept["id"], "kind": kind})
                generated_exact_rows.append(guarded)
                raw_generated_exact_rows.append(raw)

    corpus_by_class = {}
    raw_corpus_by_class = {}
    for query_class in sorted({entry["class"] for entry in corpus_entries}):
        corpus_by_class[query_class] = metrics([row for row in guarded_corpus if next(entry for entry in corpus_entries if entry["id"] == row["id"])["class"] == query_class])
        raw_corpus_by_class[query_class] = metrics([row for row in raw_corpus if next(entry for entry in corpus_entries if entry["id"] == row["id"])["class"] == query_class])

    # Query latency: single-query encode path, after warmup, on the actual hardware.
    all_queries = [entry["query"] for entry in paraphrase_entries] + [entry["query"] for entry in corpus_entries]
    for query in all_queries[:5]:
        embedder.embed_one(query)
    latency_ms = []
    for _ in range(args.latency_repeats):
        for query in all_queries:
            start = time.perf_counter()
            embedder.embed_one(query)
            latency_ms.append((time.perf_counter() - start) * 1000.0)

    guarded_metrics = {
        "paraphraseAll": metrics(guarded_paraphrase),
        "paraphraseLowOverlap": metrics(guarded_low),
        "existingAll": metrics(guarded_corpus),
        "existingByClass": corpus_by_class,
        "exactExisting": metrics(guarded_exact_existing),
        "exactGenerated": metrics(generated_exact_rows),
    }
    raw_metrics = {
        "paraphraseAll": metrics(raw_paraphrase),
        "paraphraseLowOverlap": metrics(raw_low),
        "existingAll": metrics(raw_corpus),
        "existingByClass": raw_corpus_by_class,
        "exactExisting": metrics(raw_exact_existing),
        "exactGenerated": metrics(raw_generated_exact_rows),
    }
    baseline_metrics = {
        "paraphraseAll": baseline["paraphrase"]["all"],
        "paraphraseLowOverlap": baseline["paraphrase"]["lowOverlap"],
        "existingAll": baseline["existingBenchmark"]["all"],
        "exactExisting": baseline["exactMatchControls"]["existing"]["metrics"],
        "exactGenerated": baseline["exactMatchControls"]["generatedIdentifierNameAlias"]["metrics"],
    }
    deltas = {
        key: {
            "recallAt5": guarded_metrics[key]["recallAt5"] - baseline_metrics[key]["recallAt5"],
            "mrrAt5": guarded_metrics[key]["mrrAt5"] - baseline_metrics[key]["mrrAt5"],
        }
        for key in baseline_metrics
    }
    exact_no_regression = (
        guarded_metrics["exactExisting"]["recallAt5"] >= baseline_metrics["exactExisting"]["recallAt5"]
        and guarded_metrics["exactExisting"]["mrrAt5"] >= baseline_metrics["exactExisting"]["mrrAt5"]
        and guarded_metrics["exactGenerated"]["recallAt5"] >= baseline_metrics["exactGenerated"]["recallAt5"]
        and guarded_metrics["exactGenerated"]["mrrAt5"] >= baseline_metrics["exactGenerated"]["mrrAt5"]
        and all(row["top1Exact"] for row in guarded_exact_existing)
        and all(row["top1Exact"] for row in generated_exact_rows)
    )
    overall_no_regression = (
        guarded_metrics["existingAll"]["recallAt5"] >= baseline_metrics["existingAll"]["recallAt5"]
        and guarded_metrics["existingAll"]["mrrAt5"] >= baseline_metrics["existingAll"]["mrrAt5"]
        and guarded_metrics["paraphraseAll"]["recallAt5"] >= baseline_metrics["paraphraseAll"]["recallAt5"]
        and guarded_metrics["paraphraseAll"]["mrrAt5"] >= baseline_metrics["paraphraseAll"]["mrrAt5"]
    )
    thresholds = {
        "lowOverlapRecallDeltaAtLeast": RECALL_DELTA_THRESHOLD,
        "lowOverlapMrrDeltaAtLeast": MRR_DELTA_THRESHOLD,
        "lowOverlapRecallDeltaPass": deltas["paraphraseLowOverlap"]["recallAt5"] >= RECALL_DELTA_THRESHOLD,
        "lowOverlapMrrDeltaPass": deltas["paraphraseLowOverlap"]["mrrAt5"] >= MRR_DELTA_THRESHOLD,
        "exactNoRegressionPass": exact_no_regression,
        "overallNoRegressionPass": overall_no_regression,
        "quantitativeAdoptionPass": (
            deltas["paraphraseLowOverlap"]["recallAt5"] >= RECALL_DELTA_THRESHOLD
            and deltas["paraphraseLowOverlap"]["mrrAt5"] >= MRR_DELTA_THRESHOLD
            and exact_no_regression
            and overall_no_regression
        ),
        "operationalAcceptable": None,
        "licensingAcceptable": None,
        "reproducibilityAcceptable": None,
        "inspectabilityAcceptable": None,
        "adoptionJustified": None,
        "note": "Qualitative gates require human review. Quantitative pass alone does not authorize production integration.",
    }

    result = {
        "mode": "comparison",
        "generator": "Lattice.M93.EmbeddingEvaluation",
        "fixtureSha256": fixture_sha256,
        "baselineSha256": sha256_file(args.baseline),
        "baselineGenerator": baseline["generator"],
        "baselineImplementation": baseline["implementation"],
        "lowOverlapThreshold": LOW_OVERLAP_THRESHOLD,
        "lowOverlapIds": low_overlap_ids,
        "modelManifest": embedder.manifest["model"],
        "semanticsReference": embedder.manifest["semanticsReference"],
        "verifiedFiles": embedder.verified_files,
        "documentTextPolicy": "preferred name + declared aliases + definition; identifiers handled by exact guard",
        "candidatePolicy": "guarded: exact id/preferred/alias matches first, then cosine; raw: cosine only",
        "selfTest": tests,
        "packageVersions": package_versions(),
        "hardware": hardware_info(),
        "operational": {
            "modelLoadSeconds": load_seconds,
            "documentEmbeddingSeconds": doc_seconds,
            "documentCount": len(paraphrase_texts) + len(physics_texts),
            "modelDiskBytes": sum(item["bytes"] for item in embedder.verified_files.values()),
            "onnxModelBytes": embedder.verified_files["model.onnx"]["bytes"],
            "rssBeforeBytes": rss_before,
            "rssAfterDocumentEmbeddingBytes": rss_after_docs,
            "rssPeakBytes": (
                resource.getrusage(resource.RUSAGE_SELF).ru_maxrss * 1024 if resource is not None else None
            ),
            "queryLatencyMilliseconds": {
                "samples": len(latency_ms),
                "median": percentile(latency_ms, 0.50),
                "p95": percentile(latency_ms, 0.95),
                "max": max(latency_ms) if latency_ms else 0.0,
            },
            "onnxThreads": {"intraOp": 1, "interOp": 1},
            "providers": embedder.session.get_providers(),
            "documentDiagnostics": {"paraphrase": paraphrase_doc_diag, "physics": physics_doc_diag},
        },
        "baselineMetrics": baseline_metrics,
        "guardedEmbeddingMetrics": guarded_metrics,
        "rawEmbeddingMetrics": raw_metrics,
        "deltasGuardedMinusLexical": deltas,
        "thresholds": thresholds,
        "rows": {
            "guardedParaphrase": guarded_paraphrase,
            "rawParaphrase": raw_paraphrase,
            "guardedExisting": guarded_corpus,
            "rawExisting": raw_corpus,
            "guardedExactExisting": guarded_exact_existing,
            "rawExactExisting": raw_exact_existing,
            "guardedExactGenerated": generated_exact_rows,
            "rawExactGenerated": raw_generated_exact_rows,
        },
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {args.output}")
    print(json.dumps({"baselineMetrics": baseline_metrics, "guardedEmbeddingMetrics": guarded_metrics, "deltas": deltas, "thresholds": thresholds}, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
