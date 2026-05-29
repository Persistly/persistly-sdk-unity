#!/usr/bin/env bash
set -euo pipefail

DRY_RUN=0
VERSION="1.0.0"
if [[ "${1:-}" == "--dry-run" ]]; then
  DRY_RUN=1
elif [[ "${1:-}" != "" ]]; then
  VERSION="$1"
fi
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
  if [[ "$DRY_RUN" == "0" ]]; then
    tar -czf "$ARCHIVE_PATH" .
  fi
)

if [[ "$DRY_RUN" == "1" ]]; then
  echo "dry-run package staged at $STAGE_DIR"
else
  echo "$ARCHIVE_PATH"
fi
