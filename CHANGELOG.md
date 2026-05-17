# Changelog

## 1.0.0

- First stable public Unity SDK release.
- Adds `PersistlyGameSaves` as the recommended game-friendly facade for named slots, local-first saves, profile account data, profile sessions, due-slot sync, force sync, and explicit conflict states.
- Reconciles an existing remote slot when a local slot is missing its `CharacterSaveId`, preventing duplicate-slot errors after reinstall, cache loss, or local state drift.
- Adds profile creation, profile session headers, profile-scoped character load/sync, runtime config, and local autosave draft helpers.
- Keeps raw save create/load/sync available as advanced APIs.
- Adds dev/test live parity smoke tooling for real API validation without committing runtime keys.
- Documents `profileSaveId`, `profileSessionToken`, character `saveId`, and integer save `version` semantics through the pinned contract bundle.
- Pins `persistly-contract-v0.3.0`.

## 0.1.0

- Initial Unity SDK preview for Persistly create, load, sync, cache helpers, typed sync status, and structured runtime errors.
- Includes a sample project and Last Beacon scene for engine validation.
- Pins the initial Persistly contract bundle for OpenAPI, examples, and runtime payload limits.
