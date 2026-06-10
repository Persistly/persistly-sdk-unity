# Auth-Required Unity Template

Use this template when your game requires Firebase or Supabase sign-in before cloud sync.

`AuthRequired` mode still lets the game save and load local data before sign-in. Cloud sync calls return `AuthRequired` until the player signs in, and the SDK does not create an anonymous remote account.

Provider tokens are exchanged only through Persistly auth-session endpoints. Normal save, load, and sync calls use the returned Persistly `accountId` and `accountSessionToken`.

## Files

- `PersistlySaveService.cs` configures auth-required mode, signs in with a Firebase ID token or Supabase access token, saves locally, syncs, and signs out.
- `UsageExample.cs` shows local save before sign-in, sign-in, sync, and local sign-out.
