# Anonymous-First Connect-Later Unity Template

Use this template when your Unity game lets players start immediately, then offers Firebase, Supabase, or Auth0 account connection after progress exists.

Persistly anonymous-first is a Persistly account mode. It is not Firebase Anonymous Auth, Supabase anonymous sign-in, or an Auth0 guest user. The player can save locally and sync to an anonymous Persistly account before they ever open your provider login UI.

Persistly saves locally first. Cloud sync can create an anonymous Persistly account, and `ConnectWith...TokenAsync` later links that account to a provider token from your game's Firebase, Supabase, or Auth0 SDK. If the provider is already linked to another Persistly account, Persistly returns `account_auth_conflict` as `PersistlyAccountAuthConflictError`; the SDK keeps the current local progress so your game can ask the player before switching.

Persistly does not provide login UI in this phase. Your game owns the Firebase, Supabase, or Auth0 sign-in flow.

Provider tokens are only used for sign-in or connect calls. Normal `SaveDataAsync`, `LoadDataAsync`, and `ForceSyncDataAsync` calls continue using the Persistly `accountId` plus `accountSessionToken`; do not store provider tokens in save data or send them with normal sync.
