#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${PERSISTLY_RUNTIME_KEY:-}" ]]; then
  echo "PERSISTLY_RUNTIME_KEY must be set to a dev/test runtime key." >&2
  exit 1
fi

if [[ -z "${UNITY_BIN:-}" ]]; then
  echo "UNITY_BIN must point to the Unity editor binary." >&2
  exit 1
fi

if [[ ! -x "$UNITY_BIN" ]]; then
  echo "Unity binary not executable: $UNITY_BIN" >&2
  exit 1
fi

PROJECT_PATH="${PROJECT_PATH:-$(pwd)/SampleProject}"
RESULTS_PATH="${RESULTS_PATH:-$(pwd)/TestResults-live-smoke.xml}"
LOG_PATH="${LOG_PATH:-$(pwd)/unity-live-smoke.log}"

"$UNITY_BIN" \
  -batchmode \
  -nographics \
  -projectPath "$PROJECT_PATH" \
  -runTests \
  -testPlatform EditMode \
  -testFilter Persistly.Unity.LastBeacon.Tests.PersistlyLiveSmokeTests.LiveGameSavesFacadeCreatesLoadsAndSyncsAccountAndSlot \
  -testResults "$RESULTS_PATH" \
  -logFile "$LOG_PATH" \
  -quit

echo "Unity live smoke results: $RESULTS_PATH"
echo "Unity live smoke log: $LOG_PATH"
