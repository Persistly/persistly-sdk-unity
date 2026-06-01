# Account + Slots Unity Template

Use this template when your game has sign-in, cross-device restore, or a backend that stores the Persistly account session for the player.

The token export path is explicit. Send `AccountId` and `AccountSessionToken` to your trusted backend over HTTPS, and never log the session token.
For anonymous device transfer without your own account system, use the transfer-code helpers. Transfer codes are short-lived, one-use codes for moving the current Persistly account session to another device; they are not authentication credentials.

## Files

- `PersistlySaveService.cs` wraps account attach/export, transfer-code attach, and named slot save/sync.
- `UsageExample.cs` shows backend-backed restore plus transfer-code restore.
