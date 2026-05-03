#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[1]
BUNDLE = ROOT / "contracts" / "persistly-contract-v0.1.0"
MANIFEST = BUNDLE / "manifest.json"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main() -> int:
    if not MANIFEST.exists():
        print(f"missing manifest: {MANIFEST}", file=sys.stderr)
        return 1

    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    expected_bundle = "persistly-contract-v0.1.0"
    if manifest.get("bundle") != expected_bundle:
        print(f"unexpected bundle: {manifest.get('bundle')}", file=sys.stderr)
        return 1

    files = manifest.get("files")
    if not isinstance(files, list):
        print("manifest files entry must be a list", file=sys.stderr)
        return 1

    failures: list[str] = []
    for entry in files:
        if not isinstance(entry, dict):
            failures.append("manifest entry is not an object")
            continue

        relative_path = entry.get("path")
        expected_sha = entry.get("sha256")
        expected_bytes = entry.get("bytes")
        if not isinstance(relative_path, str) or not isinstance(expected_sha, str) or not isinstance(expected_bytes, int):
            failures.append(f"invalid manifest entry: {entry!r}")
            continue

        path = BUNDLE / relative_path
        if not path.exists():
            failures.append(f"missing file: {path}")
            continue

        actual_bytes = path.read_bytes()
        if len(actual_bytes) != expected_bytes:
            failures.append(f"byte count mismatch: {path} expected {expected_bytes} got {len(actual_bytes)}")
            continue

        actual_sha = hashlib.sha256(actual_bytes).hexdigest()
        if actual_sha != expected_sha:
            failures.append(f"sha mismatch: {path} expected {expected_sha} got {actual_sha}")

    if failures:
        for failure in failures:
            print(failure, file=sys.stderr)
        return 1

    print(f"Persistly contract bundle is valid at {BUNDLE}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

