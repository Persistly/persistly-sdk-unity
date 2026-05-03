# Persistly Unity SDK Context

## Brainstorm Backlog

- Add a Unity editor test assembly once the package is opened in a real Unity project.
- Add a file-backed cache option if the Unity track needs offline persistence beyond the in-memory starter cache.

## Approved Ideas

- Keep the public Unity SDK boundary limited to create, load, and sync save operations.
- Use a UnityWebRequest transport abstraction so the package stays native to Unity while still being testable.
- Keep request payloads explicit as JSON object strings and expose parsed response JSON as `JsonElement` values.
- Include an in-memory cache and infer `baseVersion` from cache for sync when the caller does not provide one.
- Pin the SDK to contract bundle `persistly-contract-v0.1.0` and validate the bundle with a local script.

## Implemented Features

- Package-style Unity layout under `Runtime/`, `examples/`, `contracts/`, and `Scripts/`.
- Runtime client, DTOs, errors, cache, and transport abstraction for the Persistly runtime API.
- Local contract validation script that checks the pinned manifest, checksums, and bundle shape.

