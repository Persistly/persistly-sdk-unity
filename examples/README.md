# Minimal Usage Example

This folder holds the smallest Unity-facing usage sample for the Persistly Unity SDK.

The example uses the facade-first game-saves path:

- `PersistlyGameSavesSettings`
- `PersistlyGameSaves`
- `FilePersistlyGameSavesStore`
- `PersistlySaveSlotOptions`

It saves locally first, loads from local storage, then calls `ForceSyncAsync` explicitly. The SDK does not start automatic background sync timers.

Runtime delete helpers also exist on same facade:

- `DeleteSlotAsync("autosave")` for one slot
- `DeleteAccountAsync()` for whole stored account namespace
