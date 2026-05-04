# Persistly Unity SDK

Unity runtime SDK for Persistly profile saves, profile sessions, character save-sync, and local autosave support.

Persistly is a lightweight cloud save backend for games. The recommended Unity flow is:

1. Create a profile with the first character.
2. Persist `profileSaveId`, `profileSessionToken`, and character `saveId` locally.
3. Load and sync characters through the profile session.
4. Keep gameplay state local first, then sync remotely at safe intervals or on explicit player action.

Raw create/load/sync save calls remain available for advanced migrations, but new games should start with profile sessions.

## Install

Add the package from the public Persistly Unity repository:

```text
https://github.com/Persistly/persistly-sdk-unity.git?path=/
```

In Unity, open Package Manager, choose **Add package from git URL**, paste the URL, then configure a `ps_test_...` or `ps_live_...` runtime key in your game code or inspector.

## Quickstart

```csharp
using Persistly.Unity;

var client = new PersistlyClient(new PersistlyClientOptions("ps_test_..."));

var created = await client.CreateProfileAsync(new PersistlyCreateProfileRequest(
    accountDataJson: "{\"diamonds\":20}",
    characterMetadataJson: "{\"characterName\":\"Ayla\",\"slot\":1}",
    characterStateJson: "{\"gold\":100,\"level\":1}",
    profileMetadataJson: "{\"displayName\":\"Ayla\"}",
    playerRef: "player-184"));

// Store these locally. They are the resume material for this player/profile.
var profileSaveId = created.ProfileSaveId;
var profileSessionToken = created.ProfileSessionToken;
var characterSaveId = created.Character.Save.SaveId;

var loaded = await client.LoadProfileCharacterAsync(
    profileSaveId,
    profileSessionToken,
    characterSaveId);

var result = await client.SyncProfileCharacterAsync(
    profileSaveId,
    profileSessionToken,
    characterSaveId,
    new PersistlySyncSaveRequest(
        stateJson: "{\"gold\":120,\"level\":2}",
        baseVersion: loaded.Version,
        metadataJson: "{\"characterName\":\"Ayla\",\"slot\":1}"));

if (result.Status == PersistlySyncStatus.Conflict)
{
    // result.Save is the canonical server character save. Reconcile intentionally.
}
```

## Runtime Surface

The package includes:

- `PersistlyClient`
- `CreateProfileAsync`, `LoadProfileAsync`, `CreateProfileCharacterAsync`, `LoadProfileCharacterAsync`, and `SyncProfileCharacterAsync`
- advanced raw `CreateSaveAsync`, `LoadSaveAsync`, and `SyncSaveAsync`
- `GetRuntimeConfigAsync` for server-provided sync policy
- typed runtime errors, including forbidden profile-session failures
- `InMemoryPersistlySaveCache`
- `InMemoryPersistlyAutosaveDraftStore`
- `FilePersistlyAutosaveDraftStore`
- `PersistlyAutosaveManager`
- `UnityWebRequestTransport`
- Unity-safe JSON parsing and serialization without `System.Text.Json`

## Profile Sessions

Profile endpoints require `profileSessionToken`. The SDK sends it as `X-Persistly-Profile-Session` for profile and character routes.

`playerRef` and `externalProfileRefJson` are optional developer references. `externalProfileRefJson` must be a JSON object such as `{"provider":"auth0","subject":"auth0|user_123"}`. These references are not authentication tokens, not ownership proof, and not public lookup APIs. Store `profileSaveId` and `profileSessionToken` locally or in your own trusted backend.

## Autosave

`PersistlyAutosaveManager` lets games write every state change to local storage while respecting Persistly remote-sync policy:

- local changes are stored immediately
- remote sync can be throttled by `minRemoteSyncIntervalSeconds`
- explicit sync buttons can use force sync and honor `forceSyncCooldownSeconds`
- if the game closes before remote sync, the local draft is still available

Use `FilePersistlyAutosaveDraftStore` with `Application.persistentDataPath` for real players and `InMemoryPersistlyAutosaveDraftStore` for tests.

## Contract Bundle

This repo pins `persistly-contract-v0.2.0` under `contracts/`.
The bundle is authoritative for request/response semantics, routes, and runtime limits.

## Validation

Run the local bundle check from the repo root:

```bash
python3 Scripts/validate_contract.py
```

Run the Unity edit-mode suite:

```bash
UNITY_BIN="/Applications/Unity/Unity-6000.4.2f1/Unity.app/Contents/MacOS/Unity"
"$UNITY_BIN" \
  -batchmode \
  -projectPath "$(pwd)/SampleProject" \
  -runTests \
  -testPlatform EditMode \
  -logFile -
```

## Examples

- `examples/MinimalUsage.cs` for a minimal profile/session snippet
- `SampleProject/Assets/LastBeacon/` for the playable endless-idle sample
- `SampleProject/Assets/Scenes/LastBeacon.unity` for the generated demo scene

## Release Checklist

- validate the pinned contract bundle
- run the Unity edit-mode suite
- open `SampleProject` and verify `Last Beacon` against a real runtime key
- keep sample defaults aligned with `https://api.persistly.app`
