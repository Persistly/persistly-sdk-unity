# Changelog

## 1.3.0

- Clarifies anonymous-first Auth Bridge conflict handling for Firebase, Supabase, and Auth0 connect-later flows.
- Updates examples and templates to show that `PersistlyAccountAuthConflictError` preserves current local progress.
- Documents the safe player choices when a provider identity already belongs to another Persistly account: keep local progress, retry with another provider account, or explicitly discard local state before using the provider-linked account.

## 1.2.0

- Adds explicit connect-later helpers for anonymous-first games: `ConnectWithFirebaseTokenAsync`, `ConnectWithSupabaseTokenAsync`, `ConnectWithAuth0TokenAsync`, and `ConnectProviderAsync`.
- Adds an anonymous-first Auth Bridge template showing how to keep local/cloud progress before sign-in and connect Firebase later without silently overwriting saves.
- Clarifies Auth Bridge docs so provider tokens are used only for sign-in/connect exchanges while normal save/load/sync calls continue on Persistly account sessions.

## 1.1.0

- Adds Auth Bridge helpers for Firebase Auth, Supabase Auth, and Auth0.
- Adds `AuthRequired` account mode so games can keep local saves before sign-in while cloud sync waits for a provider token exchange.
- Adds provider sign-in, provider linking, linked-provider listing, and provider-specific error types.
- Adds Auth Bridge examples/templates while keeping normal save/load/sync calls on Persistly account sessions.

## 1.0.0

- First stable public Unity SDK release.
- Adds `PersistlyGameSaves` as the recommended game-friendly facade for named slots, local-first saves, account data, account sessions, due-slot sync, force sync, and explicit conflict states.
- Reconciles an existing remote slot when local slot state is missing after reinstall, cache loss, or local state drift.
- Adds account creation, account session headers, account-scoped slot load/sync, runtime config, and local autosave draft helpers.
- Keeps advanced integrations on the account/slot runtime client without exposing legacy raw-save compatibility as the public release path.
- Adds dev/test live parity smoke tooling for real API validation without committing runtime keys.
- Documents account sessions, slot ids, and canonical slot revision semantics through the pinned contract bundle.
- Pins `persistly-contract-v0.4.0`.

## 0.1.0

- Initial Unity SDK preview for Persistly create, load, sync, cache helpers, typed sync status, and structured runtime errors.
- Includes a sample project and Last Beacon scene for engine validation.
- Pins the initial Persistly contract bundle for OpenAPI, examples, and runtime payload limits.
