# Persistly Unity SDK

Unity runtime SDK for Persistly game-friendly saves, profile sessions, character save-sync, and local autosave support.

Persistly is a lightweight cloud save backend for games. The recommended Unity flow is:

1. Configure `PersistlyGameSaves` once with a runtime key and player reference.
2. Save and load gameplay slots locally through `PersistlyGameSaves.Shared`.
3. Call `ForceSyncAsync` from explicit player actions or safe sync points.
4. Use the advanced runtime client only when you need raw profile/session or migration APIs.

This release includes a local-first facade shell. Remote profile API wiring will be added separately.

## Install

Add the package from the public Persistly Unity repository:

```text
https://github.com/Persistly/persistly-sdk-unity.git?path=/
```

In Unity, open Package Manager, choose **Add package from git URL**, paste the URL, then configure a `ps_test_...` or `ps_live_...` runtime key in your game code or inspector.

## Quickstart

```csharp
using Persistly.Unity;

await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings(
    runtimeKey: "ps_test_...",
    playerRef: "player-184",
    syncIntervalSeconds: 60));

await PersistlyGameSaves.Shared.SaveSlotAsync("slot-1", new PlayerSaveState
{
    Gold = 120,
    Level = 2
});

var loaded = await PersistlyGameSaves.Shared.LoadSlotAsync<PlayerSaveState>("slot-1");
var state = loaded.State;

var sync = await PersistlyGameSaves.Shared.ForceSyncAsync("slot-1");

if (sync.Status == PersistlySlotStatus.Conflict)
{
    // Remote conflict handling will be surfaced here when cloud sync is wired.
}

[System.Serializable]
public sealed class PlayerSaveState
{
    public int Gold;
    public int Level;
}
```

## Advanced Runtime Client

Use `PersistlyClient` directly for profile sessions, character save-sync, migrations, and lower-level API access.

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
- `PersistlyGameSaves`
- `PersistlyGameSavesSettings`
- `PersistlySlotStatus`
- `PersistlySlotResult`
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
