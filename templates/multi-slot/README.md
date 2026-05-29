# Multi-Slot Unity Template

Use this template when your game has manual saves, campaigns, or a slot-select screen. Each save uses a stable developer slot id such as `campaign-1` or `challenge`.

## Files

- `PersistlySaveService.cs` wraps named slot save, load, list, and sync calls.
- `UsageExample.cs` shows a slot-select style flow.

Use stable ids that do not change when the player renames a save.

