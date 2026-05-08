# Installation

Buttr for Unity ships as a UPM package. Requires Unity 6.0 or later.

## Install via Package Manager

Buttr.Unity declares a UPM dependency on `com.crumpetlabs.buttr` (Buttr.Core). Unity's package manager **does not auto-resolve git-URL dependencies**, so Core has to be installed first.

1. `Window > Package Manager`.
2. `+` (top-left) → **Install package from git URL**.
3. Paste the Buttr.Core URL:

   ```
   https://github.com/Crumpet-Labs/Buttr.Core.git?path=package
   ```

4. Repeat for Buttr.Unity:

   ```
   https://github.com/Crumpet-Labs/Buttr.Unity.git?path=Assets/Plugins/Buttr
   ```

Buttr.Core lands under `Packages/com.crumpetlabs.buttr/`. Buttr.Unity references it transitively through its asmdef — you're ready to go.

If you'd rather edit `Packages/manifest.json` directly:

```json
"dependencies": {
  "com.crumpetlabs.buttr": "https://github.com/Crumpet-Labs/Buttr.Core.git?path=package",
  "com.crumpetlabs.buttr.unity": "https://github.com/Crumpet-Labs/Buttr.Unity.git?path=Assets/Plugins/Buttr"
}
```

Order in the manifest doesn't matter — UPM only needs both entries to be present at resolve time.

## Install a specific version

Append `#<tag>` to pin either package to a release:

```
https://github.com/Crumpet-Labs/Buttr.Core.git?path=package#v1.3.4
https://github.com/Crumpet-Labs/Buttr.Unity.git?path=Assets/Plugins/Buttr#v2.4.2
```

Tags follow [Semantic Versioning](https://semver.org/). Each Buttr.Unity release pins a specific Buttr.Core minimum in its `package.json` — see the Unity [CHANGELOG](https://github.com/Crumpet-Labs/Buttr.Unity/blob/main/Assets/Plugins/Buttr/CHANGELOG.md) for the pairing per release.

## Update

Package Manager → select **Buttr for Unity** or **Buttr** (Core) → **Update** if a newer tag is available, or re-enter the git URL without a tag suffix to track the latest `main`. Both packages are updated independently.

## Editor-only assemblies

The Buttr.Core and Buttr.Injection DLLs ship with the Buttr.Core package and live at `Packages/com.crumpetlabs.buttr/Runtime/Lib/`. You don't need to wire them into your own asmdefs — reference `Buttr.Unity`, which transitively exposes `Buttr.Core` and `Buttr.Injection` through its asmdef.

If you need direct access without going through `Buttr.Unity`, reference the Core asmdefs (`Buttr.Core`, `Buttr.Injection`) from your own asmdef.

The Roslyn analyzer (`Buttr.Core.Analyzers.dll`) and source generator stay in Buttr.Unity's `Analyser/` folder — Roslyn analyzer packaging is Unity-specific and doesn't ship in the Core UPM package.

## Requirements

- Unity 6.0 or later
- .NET Standard 2.1

## Platform support

Tested on: Windows, macOS, Linux, iOS, Android.
Not yet tested: WebGL, consoles. Additional validation recommended before shipping on those platforms.
