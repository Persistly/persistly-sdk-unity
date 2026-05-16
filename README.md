# Persistly Unity SDK

Unity runtime SDK for Persistly profile-backed, local-first game saves.

The recommended Unity flow is facade-first:

1. Configure `PersistlyGameSaves` once with a runtime key and optional restore identifiers.
2. Save and load named slots locally through `PersistlyGameSaves.Shared`.
3. Call `ForceSyncAsync`, `SyncDueSlotsAsync`, or `SyncDueProfileAsync` from explicit lifecycle/safe-sync points.
4. Use `PersistlyClient` directly only for advanced runtime API access.

This package is `0.10.0` and pins `persistly-contract-v0.3.0`.

## Install

Add the package from the public Persistly Unity repository:

```text
https://github.com/Persistly/persistly-sdk-unity.git?path=/
```

In Unity, open Package Manager, choose **Add package from git URL**, paste the URL, then configure a `ps_test_...` or `ps_live_...` runtime key in your game code or inspector.

## Quickstart

```csharp
using Persistly.Unity;
using UnityEngine;

await PersistlyGameSaves.ConfigureAsync(new PersistlyGameSavesSettings("ps_test_...")
{
    PlayerRef = "player-184",
    Store = new FilePersistlyGameSavesStore(Application.persistentDataPath)
});

await PersistlyGameSaves.Shared.SaveSlotAsync("autosave", new PlayerSaveState
{
    Gold = 120,
    Level = 2
}, new PersistlySaveSlotOptions
{
    MetadataJson = "{\"characterName\":\"Ayla\"}"
});

var loaded = await PersistlyGameSaves.Shared.LoadSlotAsync<PlayerSaveState>("autosave");
if (loaded.Status == PersistlySlotStatus.LocalFound)
{
    var state = loaded.State;
}

var sync = await PersistlyGameSaves.Shared.ForceSyncAsync("autosave");
if (sync.Status == PersistlySlotStatus.Conflict)
{
    var inspect = PersistlyGameSaves.Shared.InspectSlot("autosave");
    // inspect.StateJson is local gameplay state; inspect.CloudStateJson is the canonical cloud version.
}

[System.Serializable]
public sealed class PlayerSaveState
{
    public int Gold;
    public int Level;
}
```

`SaveSlotAsync` writes local gameplay state immediately. The first `ForceSyncAsync`, `SyncDueSlotsAsync`, or `SyncDueAsync` call creates the remote Persistly profile and the matching character slot if needed.

## Profiles And Restore

`PersistlyGameSavesSettings` supports:

- `PlayerRef`
- `ExternalProfileRefJson`
- `LocalProfileKey`
- `ProfileSaveId`
- `ProfileSessionToken`

`playerRef` and `externalProfileRefJson` are optional developer references. They are not authentication, ownership proof, lookup, or recovery APIs. Cross-device restore uses explicit `ProfileSaveId` plus `ProfileSessionToken`, usually stored by your own trusted backend.

`GetProfileSession()` hides the token by default:

```csharp
var hidden = PersistlyGameSaves.Shared.GetProfileSession();
var exported = PersistlyGameSaves.Shared.GetProfileSession(includeToken: true);
```

## Account Data

Profile account data is local-first and synced separately from character slots:

```csharp
await PersistlyGameSaves.Shared.SaveAccountDataAsync(new AccountState { Diamonds = 25 });
await PersistlyGameSaves.Shared.PatchAccountDataAsync("{\"diamonds\":30,\"oldKey\":null}");
await PersistlyGameSaves.Shared.ForceSyncProfileAsync();
```

Profile account-data sync preserves server-owned `characterSlots`; it never rewrites slot references from account data.

## Slots And Conflicts

Use named slots for gameplay saves:

- `SaveSlotAsync` writes local state immediately.
- `LoadSlotAsync`, `ListSlots`, and `InspectSlot` are local-only.
- `ForceSyncAsync` syncs one slot and respects manual cooldown unless `BypassCooldown` is set.
- `SyncDueSlotsAsync` syncs dirty slots only when the runtime policy allows it.
- `ArchiveSlotAsync` archives remotely before marking a local slot archived.
- No automatic background timers are started by the SDK.

Conflicts keep local and cloud state separate. Local gameplay state is never overwritten automatically. Use:

- `AcceptCloudVersionAsync`
- `OverwriteCloudVersionAsync`
- `KeepLocalForLaterAsync`

## Advanced Runtime Client

`PersistlyClient` exposes the underlying v0.3.0 runtime API:

- profile-only `CreateProfileAsync`
- optional initial character creation
- `CreateProfileCharacterAsync`
- `LoadProfileCharacterAsync`
- `SyncProfileCharacterAsync`
- `SyncProfileAccountDataAsync`
- `ArchiveProfileCharacterAsync`
- typed `slot_already_exists` and `character_archived` errors
- advanced raw `CreateSaveAsync`, `LoadSaveAsync`, and `SyncSaveAsync`

Profile character metadata is built with SDK-owned `_persistly.slotKey`; developer metadata must not provide `_persistly` directly.

## Contract Bundle

This repo pins `persistly-contract-v0.3.0` under `contracts/`.
The bundle is authoritative for request/response semantics, routes, and runtime limits.

## Validate Local Changes

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

## Live Parity Smoke

Run this only with a dev/test runtime key. It creates a temporary profile and `autosave` character slot through `PersistlyGameSaves`, verifies local load, runtime config, force sync, due-slot sync, profile account-data sync, and exported profile session data.

```bash
UNITY_BIN="/Applications/Unity/Unity-6000.4.2f1/Unity.app/Contents/MacOS/Unity" \
PERSISTLY_RUNTIME_KEY=ps_test_replace_me \
Scripts/live_smoke.sh
```

Optional:

```bash
PERSISTLY_API_BASE=https://stage-api.persistly.app \
UNITY_BIN="/path/to/Unity" \
PERSISTLY_RUNTIME_KEY=ps_test_replace_me \
Scripts/live_smoke.sh
```

## Examples

- `examples/MinimalUsage.cs` for a minimal facade-first snippet
- `SampleProject/Assets/LastBeacon/` for the playable endless-idle sample
- `SampleProject/Assets/Scenes/LastBeacon.unity` for the generated demo scene
