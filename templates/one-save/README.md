# One-Save Unity Template

Use this template when your Unity game has one current save. It uses the default `autosave` slot through `SaveDataAsync`, `LoadDataAsync`, and `ForceSyncDataAsync`.

## Files

- `PersistlySaveService.cs` wraps the Persistly facade behind game-shaped methods.
- `UsageExample.cs` shows where to call the service from your own scripts.

Call `ConfigureAsync` once during startup, call `SaveAsync` whenever local state changes, and call `SyncAsync` from deliberate lifecycle points such as checkpoint, pause, manual save, or application background.

