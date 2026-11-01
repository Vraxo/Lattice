#!/usr/bin/env python3
"""Provision the M9.3 evaluation-only model artifact.

This script is intentionally standard-library-only. It downloads the pinned mirror
content through the GitHub contents API, verifies every SHA-256 against the
committed manifest and the mirror's SHA256SUMS file, and writes the artifact under
artifacts/evaluation/m9_3/model by default. Run it once before evaluate.py; the
evaluation itself performs no network access.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys
import urllib.request
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO_ROOT = HERE.parents[2]
MANIFEST_PATH = HERE / "model-manifest.json"
DEFAULT_MODEL_DIR = REPO_ROOT / "artifacts" / "evaluation" / "m9_3" / "model"
USER_AGENT = "lattice-m9-3-evaluation"


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def download_raw(repository: str, commit: str, path: str) -> bytes:
    url = f"https://api.github.com/repos/{repository}/contents/{path}?ref={commit}"
    headers = {"Accept": "application/vnd.github.raw+json", "User-Agent": USER_AGENT}
    token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(url, headers=headers)
    with urllib.request.urlopen(request, timeout=120) as response:
        return response.read()


def verify_or_write(path: Path, data: bytes, expected_sha256: str) -> None:
    actual = sha256_bytes(data)
    if actual != expected_sha256:
        raise SystemExit(f"SHA-256 mismatch for {path}: expected {expected_sha256}, got {actual}")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--model-dir", type=Path, default=DEFAULT_MODEL_DIR)
    parser.add_argument("--force", action="store_true", help="Re-download even if a verified file already exists.")
    args = parser.parse_args()

    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    model = manifest["model"]
    repository = model["mirrorRepository"]
    commit = model["mirrorCommit"]
    mirror_path = model["mirrorPath"]
    model_dir = args.model_dir.resolve()
    model_dir.mkdir(parents=True, exist_ok=True)

    sums_entry = next(entry for entry in manifest["files"] if entry["path"] == "SHA256SUMS")
    sums_data = download_raw(repository, commit, f"{mirror_path}/SHA256SUMS")
    verify_or_write(model_dir / "SHA256SUMS", sums_data, sums_entry["sha256"])
    upstream_sums = {}
    for line in sums_data.decode("utf-8").splitlines():
        if not line.strip():
            continue
        digest, name = line.split(maxsplit=1)
        upstream_sums[name.strip()] = digest

    for entry in manifest["files"]:
        name = entry["path"]
        if name == "SHA256SUMS":
            continue
        if upstream_sums.get(name) != entry["sha256"]:
            raise SystemExit(f"Manifest and mirror SHA256SUMS disagree for {name}")
        target = model_dir / name
        if target.exists() and not args.force and sha256_bytes(target.read_bytes()) == entry["sha256"]:
            print(f"verified {name}")
            continue
        data = download_raw(repository, commit, f"{mirror_path}/{name}")
        if "bytes" in entry and len(data) != entry["bytes"]:
            raise SystemExit(f"Byte length mismatch for {name}: expected {entry['bytes']}, got {len(data)}")
        verify_or_write(target, data, entry["sha256"])
        print(f"downloaded {name} ({len(data)} bytes)")

    license_entry = manifest["licenseFile"]
    license_data = download_raw(repository, commit, license_entry["sourcePath"])
    verify_or_write(model_dir / license_entry["path"], license_data, license_entry["sha256"])
    print(f"verified {license_entry['path']}")
    print(f"Model artifact ready: {model_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
