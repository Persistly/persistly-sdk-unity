# Persistly Unity SDK

Unity runtime SDK for Persistly profile-backed, local-first game saves.

The recommended Unity flow is facade-first:

1. Configure `PersistlyGameSaves` once with a runtime key and optional restore identifiers.
2. For one-save games, use `SaveDataAsync` and `LoadDataAsync`.
3. For manual saves or characters, use named slots through `SaveSlotAsync` and `LoadSlotAsync`.
4. Call `ForceSyncDataAsync`, `ForceSyncAsync`, `SyncDueSlotsAsync`, or `SyncDueProfileAsync` from explicit lifecycle/safe-sync points.
5. Use `PersistlyClient` directly only for advanced runtime API access.

This package is `1.0.0` and pins `persistly-contract-v0.3.0`.

## Install

Add the package from the public Persistly Unity repository:

```text
https://github.com/Persistly/persistly-sdk-unity.git?path=/
```

For the stable release tag:

```text
https://github.com/Persistly/persistly-sdk-unity.git?path=/#v1.0.0
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

await PersistlyGameSaves.Shared.SaveDataAsync(new PlayerSaveState
{
    Gold = 120,
    Level = 2
}, new PersistlySaveSlotOptions
{
    MetadataJson = "{\"characterName\":\"Ayla\"}"
});

var loaded = await PersistlyGameSaves.Shared.LoadDataAsync<PlayerSaveState>();
if (loaded.Status == PersistlySlotStatus.LocalFound)
{
    var state = loaded.State;
}

var sync = await PersistlyGameSaves.Shared.ForceSyncDataAsync();
if (sync.Status == PersistlySlotStatus.Conflict)
{
    var inspect = PersistlyGameSaves.Shared.InspectData();
    // inspect.StateJson is local gameplay state; inspect.CloudStateJson is the canonical cloud version.
}

// After restoring a profile session on another device, pull the default cloud slot.
var refreshed = await PersistlyGameSaves.Shared.RefreshDataAsync();

[System.Serializable]
public sealed class PlayerSaveState
{
    public int Gold;
    public int Level;
}
```

`SaveDataAsync` writes local gameplay state immediately to the default `autosave` slot and guarantees a local profile envelope exists. The first `ForceSyncDataAsync`, `SyncDueSlotsAsync`, or `SyncDueAsync` call creates the remote Persistly profile and the matching character slot if needed.

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
var profile = PersistlyGameSaves.Shared.InspectProfile();
var accountDataJson = PersistlyGameSaves.Shared.GetAccountDataJson();
```

For explicit profile-first flows:

```csharp
await PersistlyGameSaves.Shared.CreateProfileAsync();

await PersistlyGameSaves.Shared.AttachProfileAsync(
    "sv_profile",
    "pst_profile_session");
```

Facade rules:

- `CreateProfileAsync()` creates and stores one local facade profile, then syncs it to Persistly.
- `CreateProfileAsync()` rejects if local profile or slot state already exists.
- `AttachProfileAsync()` loads an already existing Persistly profile into empty local state.
- If you want to switch players on the same device, call `ClearLocalProfileAsync()` first.

To sign out locally or wipe the current local player namespace:

```csharp
await PersistlyGameSaves.Shared.ClearLocalProfileAsync();
```

That clears the stored local profile session and all local slots for the current local namespace. If you support account switching, call `ConfigureAsync(...)` again with the next player's identity or `LocalProfileKey` after clearing local state.

To permanently remove persisted runtime profile data:

```csharp
await PersistlyGameSaves.Shared.DeleteProfileAsync();
await PersistlyGameSaves.Shared.DeleteSlotAsync("autosave");
```

Delete rules:

- `DeleteProfileAsync()` clears local state either way.
- If profile has `ProfileSaveId` plus `ProfileSessionToken`, `DeleteProfileAsync()` deletes remote profile first.
- If local slot has never synced, `DeleteSlotAsync()` removes it locally only.
- If local slot has `characterSaveId`, `DeleteSlotAsync()` deletes remote character then removes local slot state.

## Account Data

Profile account data is local-first and synced separately from character slots:

```csharp
await PersistlyGameSaves.Shared.SaveAccountDataAsync(new AccountState { Diamonds = 25 });
await PersistlyGameSaves.Shared.PatchAccountDataAsync("{\"diamonds\":30,\"oldKey\":null}");
var accountDataJson = PersistlyGameSaves.Shared.GetAccountDataJson();
await PersistlyGameSaves.Shared.ForceSyncProfileAsync();
```

`PatchAccountDataAsync` shallow-merges top-level keys. `null` removes a top-level key; arrays and nested objects are replaced by the supplied value.

Profile account-data sync preserves server-owned `characterSlots`; it never rewrites slot references from account data.

## Slots And Conflicts

Use named slots for gameplay saves:

- `DefaultSlotKey` is `autosave`.
- `SaveDataAsync`, `LoadDataAsync`, `InspectData`, `RefreshDataAsync`, `ForceSyncDataAsync`, `AcceptCloudDataAsync`, `OverwriteCloudDataAsync`, and `KeepLocalDataForLaterAsync` are convenience aliases for one-save games.
- `SaveSlotAsync` writes local state immediately.
- `LoadSlotAsync`, `ListSlots`, and `InspectSlot` are local-only.
- `ForceSyncAsync` syncs one slot and respects manual cooldown unless `BypassCooldown` is set.
- `SyncDueSlotsAsync` syncs dirty slots only when the runtime policy allows it.
- `ArchiveSlotAsync` archives remotely before marking a local slot archived.
- `DeleteSlotAsync` deletes remotely for synced slots and falls back to local-only removal for unsynced slots.
- `DeleteProfileAsync` deletes remote profile when session-backed, then clears all local slot/profile state.
- `ClearLocalProfileAsync` removes the stored local profile session and all local slots for the current namespace.
- No automatic background timers are started by the SDK.

Conflicts keep local and cloud state separate. Local gameplay state is never overwritten automatically. Use:

- `AcceptCloudDataAsync`
- `OverwriteCloudDataAsync`
- `KeepLocalDataForLaterAsync`
- `AcceptCloudVersionAsync`
- `OverwriteCloudVersionAsync`
- `KeepLocalForLaterAsync`

## Advanced Runtime Client

`PersistlyClient` exposes the underlying v0.3.0 runtime API:

- profile-only `CreateProfileAsync`
- optional initial character creation
- `CreateProfileCharacterAsync`
- `LoadProfileCharacterAsync`
- `DeleteProfileAsync`
- `DeleteProfileCharacterAsync`
- `SyncProfileCharacterAsync`
- `SyncProfileAccountDataAsync`
- `ArchiveProfileCharacterAsync`
- typed `slot_already_exists` and `character_archived` errors
- advanced raw `CreateSaveAsync`, `LoadSaveAsync`, and `SyncSaveAsync`

Profile character metadata is built with SDK-owned `_persistly.slotKey`; developer metadata must not provide `_persistly` directly.

`PersistlyClient.CreateProfileAsync(...)` is intentionally a low-level runtime API call. It always attempts remote profile creation and does not inspect local facade state. Normal game code should prefer `EnsureProfileAsync()` and slot sync through `PersistlyGameSaves`.

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

## Release Package

Build the UPM archive for a GitHub release attachment:

```bash
Scripts/package_release.sh 1.0.0
```

Release metadata lives in `UPM_RELEASE.md`.

## Examples

- `examples/MinimalUsage.cs` for a minimal facade-first snippet
- `SampleProject/Assets/LastBeacon/` for the playable endless-idle sample
- `SampleProject/Assets/Scenes/LastBeacon.unity` for the generated demo scene
