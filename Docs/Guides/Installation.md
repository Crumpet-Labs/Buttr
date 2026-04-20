# Installation

Buttr for Unity ships as a UPM package. Requires Unity 6.0 or later.

## Install via Package Manager

1. `Window > Package Manager`.
2. `+` (top-left) → **Install package from git URL**.
3. Paste:

```
https://github.com/Crumpet-Labs/Buttr.Unity.git?path=Assets/Plugins/Buttr
```

Unity resolves the package, imports the vendored `Buttr.Core.dll`, `Buttr.Injection.dll`, and the analyzer DLLs, and you're ready to go.

## Install a specific version

Append `#v2.3.0` (or any tag) to pin to a release:

```
https://github.com/Crumpet-Labs/Buttr.Unity.git?path=Assets/Plugins/Buttr#v2.3.0
```

Tags follow [Semantic Versioning](https://semver.org/) and track the matching Buttr.Core release (Unity 2.3.0 ships Buttr.Core 1.3.0).

## Update

Package Manager → select **Buttr for Unity** → **Update** (if a newer tag is available), or re-enter the git URL without a tag suffix to track the latest `main`.

## Editor-only assemblies

The vendored DLLs in `Runtime/Lib/` are referenced by Buttr's runtime assembly. You don't need to wire them into your own asmdefs — reference `Buttr.Unity` (which transitively exposes `Buttr.Core` and `Buttr.Injection`).

If you need direct access without going through `Buttr.Unity`, add `"Buttr.Core.dll"` and `"Buttr.Injection.dll"` to your asmdef's `precompiledReferences` with `"overrideReferences": true`.

## Requirements

- Unity 6.0 or later
- .NET Standard 2.1

## Platform support

Tested on: Windows, macOS, Linux, iOS, Android.
Not yet tested: WebGL, consoles. Additional validation recommended before shipping on those platforms.
