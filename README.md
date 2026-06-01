# Persistly Unity SDK

[![CI](https://github.com/Persistly/persistly-sdk-unity/actions/workflows/ci.yml/badge.svg)](https://github.com/Persistly/persistly-sdk-unity/actions/workflows/ci.yml)
[![GitHub release](https://img.shields.io/github/v/release/Persistly/persistly-sdk-unity?sort=semver)](https://github.com/Persistly/persistly-sdk-unity/releases)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-222c37)](https://unity.com/releases/editor/whats-new/2021.3.0)
[![license](https://img.shields.io/github/license/Persistly/persistly-sdk-unity.svg)](LICENSE)
[![docs](https://img.shields.io/badge/docs-persistly.app-6467f2)](https://docs.persistly.app/sdk/unity)

Unity runtime SDK for Persistly account-backed, local-first game saves.

The recommended Unity flow is facade-first:

1. Configure `PersistlyGameSaves` once with a runtime key and optional restore identifiers.
2. For one-save games, use `SaveDataAsync` and `LoadDataAsync`.
3. For manual saves or slots, use named slots through `SaveSlotAsync` and `LoadSlotAsync`.
4. Call `ForceSyncDataAsync`, `ForceSyncAsync`, `SyncDueSlotsAsync`, or `SyncDueAccountAsync` from explicit lifecycle/safe-sync points.
5. Use `PersistlyClient` directly only for advanced runtime API access.

This package is `1.0.0` and pins `persistly-contract-v0.4.0`.

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
    SlotInfoJson = "{\"slotName\":\"Ayla\"}"
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

// After restoring an account session on another device, pull the default cloud slot.
var refreshed = await PersistlyGameSaves.Shared.RefreshDataAsync();

[System.Serializable]
public sealed class PlayerSaveState
{
    public int Gold;
    public int Level;
}
```

`SaveDataAsync` writes local gameplay state immediately to the default `autosave` slot and guarantees a local account envelope exists. The first `ForceSyncDataAsync`, `SyncDueSlotsAsync`, or `SyncDueAsync` call creates the remote Persistly account and the matching slot if needed.

## Accounts And Restore

`PersistlyGameSavesSettings` supports:

- `PlayerRef`
- `ExternalAccountRefJson`
- `LocalAccountKey`
- `AccountId`
- `AccountSessionToken`

`playerRef` and `externalAccountRefJson` are optional developer references. They are not authentication or ownership proof APIs.
Do not use those fields for account lookup or account recovery. Cross-device restore uses explicit `AccountId` plus `AccountSessionToken`, usually stored by your own trusted backend.

Anonymous games can also use short-lived transfer codes when the current device already has a Persistly account session:

```csharp
var transfer = await PersistlyGameSaves.Shared.CreateTransferCodeAsync(deviceLabel: "Old phone");
ShowTransferCodeToPlayer(transfer.TransferCode, transfer.ExpiresInSeconds);

await PersistlyGameSaves.Shared.AttachWithTransferCodeAsync("P7K2D-M9Q4R", deviceLabel: "New laptop");
await PersistlyGameSaves.Shared.RefreshDataAsync();
```

Transfer codes are temporary, one-use account-session bootstrap codes. They are not authentication credentials, and `AttachWithTransferCodeAsync` requires empty local account/slot state just like `AttachAccountAsync`. If the device already has local progress for another player, call `ClearLocalAccountAsync()` only after the player chooses to replace that local state.

`GetAccountSession()` hides the token by default:

```csharp
var hidden = PersistlyGameSaves.Shared.GetAccountSession();
var exported = PersistlyGameSaves.Shared.GetAccountSession(includeToken: true);
var account = PersistlyGameSaves.Shared.InspectAccount();
var accountDataJson = PersistlyGameSaves.Shared.GetAccountDataJson();
```

For explicit account-first flows:

```csharp
await PersistlyGameSaves.Shared.CreateAccountAsync();

await PersistlyGameSaves.Shared.AttachAccountAsync(
    "acc_01HYUNITY",
    "pst_account_session");
```

Facade rules:

- `CreateAccountAsync()` creates and stores one local facade account, then syncs it to Persistly.
- `CreateAccountAsync()` rejects if local account or slot state already exists.
- `AttachAccountAsync()` loads an already existing Persistly account into empty local state.
- `CreateTransferCodeAsync()` requires a stored account session and never returns account data or a session token.
- `AttachWithTransferCodeAsync()` consumes a transfer code into empty local state and stores the returned account session.
- If you want to switch players on the same device, call `ClearLocalAccountAsync()` first.

To sign out locally or wipe the current local player namespace:

```csharp
await PersistlyGameSaves.Shared.ClearLocalAccountAsync();
```

That clears the stored local account session and all local slots for the current local namespace. If you support account switching, call `ConfigureAsync(...)` again with the next player's identity or `LocalAccountKey` after clearing local state.

To permanently remove persisted runtime account data:

```csharp
await PersistlyGameSaves.Shared.DeleteAccountAsync();
await PersistlyGameSaves.Shared.DeleteSlotAsync("autosave");
```

Delete rules:

- `DeleteAccountAsync()` clears local state either way.
- If account has `AccountId` plus `AccountSessionToken`, `DeleteAccountAsync()` deletes remote account first.
- If local slot has never synced, `DeleteSlotAsync()` removes it locally only.
- If local slot has `slotId`, `DeleteSlotAsync()` deletes remote slot then removes local slot state.

## Account Data

Account data is local-first and synced separately from slots:

```csharp
await PersistlyGameSaves.Shared.SaveAccountDataAsync(new AccountState { Diamonds = 25 });
await PersistlyGameSaves.Shared.PatchAccountDataAsync("{\"diamonds\":30,\"oldKey\":null}");
var accountDataJson = PersistlyGameSaves.Shared.GetAccountDataJson();
await PersistlyGameSaves.Shared.ForceSyncAccountAsync();
```

`PatchAccountDataAsync` shallow-merges top-level keys. `null` removes a top-level key; arrays and nested objects are replaced by the supplied value.

Account data sync preserves server-owned `slots`; it never rewrites slot references from account data.

## Templates

- `templates/one-save` for idle, casual, and one-save games.
- `templates/multi-slot` for manual saves, campaigns, and slot select screens.
- `templates/account-slots` for games with sign-in or cross-device restore.

## Slots And Conflicts

Use named slots for gameplay saves:

- `DefaultSlotId` is `autosave`.
- `SaveDataAsync`, `LoadDataAsync`, `InspectData`, `RefreshDataAsync`, `ForceSyncDataAsync`, `AcceptCloudDataAsync`, `OverwriteCloudDataAsync`, and `KeepLocalDataForLaterAsync` are convenience aliases for one-save games.
- `SaveSlotAsync` writes local state immediately.
- `LoadSlotAsync`, `ListSlotDataAsync`, and `InspectSlot` are local-only.
- `ForceSyncAsync` syncs one slot and respects manual cooldown unless `BypassCooldown` is set.
- `SyncDueSlotsAsync` syncs dirty slots only when the runtime policy allows it.
- `ArchiveSlotAsync` archives remotely before marking a local slot archived.
- `DeleteSlotAsync` deletes remotely for synced slots and falls back to local-only removal for unsynced slots.
- `DeleteAccountAsync` deletes remote account when session-backed, then clears all local slot/account state.
- `ClearLocalAccountAsync` removes the stored local account session and all local slots for the current namespace.
- No automatic background timers are started by the SDK.

Conflicts keep local and cloud state separate. Local gameplay state is never overwritten automatically. Use:

- `AcceptCloudDataAsync`
- `OverwriteCloudDataAsync`
- `KeepLocalDataForLaterAsync`
- `AcceptCloudVersionAsync`
- `OverwriteCloudVersionAsync`
- `KeepLocalForLaterAsync`

## Advanced Runtime Client

`PersistlyClient` exposes the underlying v0.4.0 runtime API:

- account-only `CreateAccountAsync`
- optional initial slot creation
- `CreateAccountSlotAsync`
- `LoadAccountSlotAsync`
- `DeleteAccountAsync`
- `DeleteAccountSlotAsync`
- `SyncAccountSlotAsync`
- `SyncAccountDataAsync`
- `ArchiveSlotAsync`
- `CreateTransferCodeAsync`
- `ConsumeTransferCodeAsync`
- typed `slot_already_exists` and `slot_archived` errors
- typed transfer-code errors such as `transfer_code_invalid`, `transfer_code_expired`, and `transfer_code_consumed`
- advanced raw `CreateSaveAsync`, `LoadSaveAsync`, and `SyncSaveAsync`

Account slot requests send `slotId`, `slotInfo`, and `data` directly. Public account and slot responses do not expose internal save ids.

`PersistlyClient.CreateAccountAsync(...)` is intentionally a low-level runtime API call. It always attempts remote account creation and does not inspect local facade state. Normal game code should prefer `EnsureAccountAsync()` and slot sync through `PersistlyGameSaves`.

## Contract Bundle

This repo pins `persistly-contract-v0.4.0` under `contracts/`.
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

Run this only with a dev/test runtime key. It creates a temporary account and `autosave` slot through `PersistlyGameSaves`, verifies local load, runtime config, force sync, due-slot sync, account data sync, and exported account session data.

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

Release slotInfo lives in `UPM_RELEASE.md`.

## Examples

- `examples/MinimalUsage.cs` for a minimal facade-first snippet
- `SampleProject/Assets/LastBeacon/` for the playable endless-idle sample
- `SampleProject/Assets/Scenes/LastBeacon.unity` for the generated demo scene
