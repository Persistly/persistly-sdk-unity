# Unity UPM Release

Persistly Unity SDK is distributed first as a Unity Package Manager Git package.

## Install URLs

Latest main branch:

```text
https://github.com/Persistly/persistly-sdk-unity.git?path=/
```

Stable release tag:

```text
https://github.com/Persistly/persistly-sdk-unity.git?path=/#v1.0.0
```

## Package

- Name: `com.persistly.sdk.unity`
- Display name: `Persistly Unity SDK`
- Version: `1.0.0`
- Unity minimum: `2021.3`
- License: `Apache-2.0`
- Docs: `https://docs.persistly.app/sdk/unity`
- Repository: `https://github.com/Persistly/persistly-sdk-unity`

## Release Artifact

Build a package archive:

```bash
Scripts/package_release.sh 1.0.0
```

This creates:

```text
dist/persistly-unity-sdk-1.0.0.tgz
```

Use the Git tag install URL as the primary public install path. Attach the `.tgz` to the GitHub release for users who want an archive.

## Pre-release Checklist

- [ ] `package.json` version is `1.0.0`.
- [ ] Runtime diagnostics send `X-Persistly-SDK-Version: 1.0.0`.
- [ ] `python3 Scripts/validate_contract.py` passes.
- [ ] Unity edit-mode tests pass.
- [ ] Live smoke passes with a dev/test runtime key.
- [ ] GitHub release/tag `v1.0.0` exists.
- [ ] Release artifact is attached to GitHub release.

## Asset Store

Do not block launch on Unity Asset Store.

Asset Store can come later after the Git/UPM release has real user feedback. If submitted later, prefer Unity's UPM publishing flow for SDK/tool packages.
