#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-1.0.0}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIST_DIR="$ROOT_DIR/dist"
STAGE_DIR="$DIST_DIR/package"
ARCHIVE_PATH="$DIST_DIR/persistly-unity-sdk-$VERSION.tgz"

rm -rf "$STAGE_DIR" "$ARCHIVE_PATH"
mkdir -p "$STAGE_DIR"

for entry in Runtime contracts examples README.md CHANGELOG.md LICENSE SECURITY.md package.json UPM_RELEASE.md; do
  cp -R "$ROOT_DIR/$entry" "$STAGE_DIR/$entry"
done

(
  cd "$STAGE_DIR"
  tar -czf "$ARCHIVE_PATH" .
)

echo "$ARCHIVE_PATH"
