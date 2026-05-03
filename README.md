# Persistly Unity SDK

Unity runtime SDK for the Persistly runtime API, plus a playable `Last Beacon` sample project.

This repository includes:

- a real Unity-compatible runtime SDK
- a generated Unity sample project under `SampleProject/`
- editor-validated create/load/sync behavior
- edit-mode tests for the SDK, local profile store, and idle-game state loop

The runtime surface stays intentionally small:

- create save
- load save by `saveId`
- sync save by `saveId`

## Install

Add the package from the public Persistly Unity repository:

```text
https://github.com/Persistly/persistly-sdk-unity.git?path=/
```

In Unity, open Package Manager, choose **Add package from git URL**, paste the URL, then configure your runtime key in game code or the Unity inspector.

## Quickstart

1. Use a `ps_test_...` key for non-production environments.
2. Create a save once and persist the returned `saveId` locally.
3. Load and sync only by `saveId`.
4. Handle conflict responses explicitly instead of assuming silent last-write wins.

## Contract Bundle

This repo pins `persistly-contract-v0.2.0` under `contracts/`.
The bundle is treated as authoritative for request/response semantics and runtime limits.

## Runtime Surface

The package includes:

- `PersistlyClient`
- request and response DTOs for create/load/sync
- typed runtime API error classes
- in-memory save cache
- UnityWebRequest transport abstraction
- Unity-safe JSON parsing and serialization without `System.Text.Json`

## Payload Shape

To stay explicit and Unity-friendly, request payloads use JSON object strings for `metadata` and `state`.
Responses expose canonical payloads as raw JSON strings for `metadata` and `state`, so game code can feed them into `JsonUtility` or its own serializers without extra glue code.

## Cache Behavior

- `InMemoryPersistlySaveCache` stores canonical saves in memory
- `PersistlyClient` stores saves returned from create/load/sync into the configured cache
- `SyncSaveAsync` can infer `baseVersion` from cache when the caller omits it

## Validation

Run the local bundle check from the repo root:

```bash
python3 Scripts/validate_contract.py
```

The script verifies:

- the pinned manifest exists
- the expected bundle layout exists
- file sizes and SHA-256 checksums match the pinned manifest

Run the real Unity validation suite:

```bash
UNITY_BIN="/Applications/Unity/Unity-6000.4.2f1/Unity.app/Contents/MacOS/Unity"
"$UNITY_BIN" \
  -batchmode \
  -projectPath "$(pwd)/SampleProject" \
  -runTests \
  -testPlatform EditMode \
  -logFile -
```

Generate the sample scene if needed:

```bash
UNITY_BIN="/Applications/Unity/Unity-6000.4.2f1/Unity.app/Contents/MacOS/Unity"
"$UNITY_BIN" \
  -batchmode \
  -projectPath "$(pwd)/SampleProject" \
  -executeMethod LastBeaconSceneBuilder.BuildSceneBatchMode \
  -logFile -
```

## Production Runtime Origin

The SDK targets `https://api.persistly.app` by default. Reserve custom origins for explicit validation infrastructure only.

## Example

See:

- `examples/MinimalUsage.cs` for a minimal SDK snippet
- `SampleProject/Assets/LastBeacon/` for the playable endless-idle sample
- `SampleProject/Assets/Scenes/LastBeacon.unity` for the generated demo scene

## Release Checklist

- validate the pinned contract bundle
- run the Unity edit-mode suite
- open `SampleProject` and verify `Last Beacon` against a real runtime key
- keep sample defaults aligned with `https://api.persistly.app`
