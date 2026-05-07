# MonoBehaviour Injection

Buttr injects MonoBehaviours via compile-time source generation. A Roslyn source generator walks every MonoBehaviour marked with `[Inject]` fields and generates the resolution code statically — so the field-injection path itself runs zero reflection at runtime. Container build itself uses minimal reflection (constructor scanning to compile factory delegates, alias mapping); resolve-time hot paths are reflection-free.

## The basics

Mark fields with `[Inject]`. The containing class must be `partial` — the source generator emits a partial counterpart containing the generated injection method.

```csharp
using Buttr.Injection;
using UnityEngine;

public partial class PlayerController : MonoBehaviour {
    [Inject] private IInputService m_Input;
    [Inject] private IAudioService m_Audio;

    private void Awake() {
        m_Input.Poll();
        m_Audio.Play("startup");
    }
}
```

**If you forget `partial`**, the `BUTTR001` analyzer flags the class at compile time.

## Running the injection

Injection doesn't happen automatically — something has to call the generated injection method on each MonoBehaviour. Buttr ships two injector components to do this for you:

Both injector components run at `Awake` with `[DefaultExecutionOrder(-9000)]` — effectively "before any normal `Awake`" — then destroy themselves once injection is done. They're accessible via Unity's Add Component menu even though the types are `internal` to the `Buttr.Unity` assembly; consumers don't reference them by code.

### SceneInjector

Add a `SceneInjector` to a GameObject in the scene. On its `Awake`, it walks every root GameObject's `MonoBehaviour`s (and their children), injects each one that implements `IInjectable`, then destroys itself.

Use for: scenes where most MonoBehaviours need injection. Put it on the scene's boot GameObject.

### MonoInjector

Add a `MonoInjector` to a GameObject for scoped injection. Exposes two serialised fields:

- `Inject Strategy` (enum):
  - `Mono` — just the target MonoBehaviour.
  - `GameObject` — all MonoBehaviours on the target's GameObject.
  - `GameObjectAndChildren` — the default; all MonoBehaviours on the GameObject and all descendants.
- `Behaviour` (optional MonoBehaviour reference) — if unset, defaults to any MonoBehaviour on the same GameObject.

On `Awake`, the `MonoInjector` runs the chosen strategy and destroys itself. There's no manual `Inject()` entry point — it's fire-and-forget.

Use for: prefabs instantiated at runtime that need their dependencies filled (put a `MonoInjector` on the prefab root — it fires on `Instantiate` → `Awake`), or isolated GameObjects in scenes that mostly don't use injection.

For prefabs that don't carry a `MonoInjector`, call `InjectionProcessorUnityExtensions` directly (below).

## Scoped injection

Pass a scope key as a `string` argument to `[Inject]` to resolve from a specific scope rather than the application container:

```csharp
public partial class InventoryUI : MonoBehaviour {
    [Inject] private IAudioService m_Audio;                      // Application container
    [Inject(Scopes.Inventory)] private IInventoryService m_Inv;  // Inventory scope
}
```

Scopes fall back to the application container if the type isn't registered in the scope. Define scope keys as constants to avoid magic strings — the `BUTTR009` analyzer flags raw string literals in `[Inject("...")]`.

```csharp
public static class Scopes {
    public const string Inventory = "inventory";
    public const string Gameplay  = "gameplay";
}
```

See the [Buttr.Core Containers doc](https://github.com/Crumpet-Labs/Buttr.Core/blob/main/Docs/Containers.md) for scope behaviour in depth.

## Injecting by hand

For cases the injector components don't cover — a manually-constructed hierarchy, a runtime-spawned object without a `MonoInjector`, a code-triggered rescan — call `InjectionProcessorUnityExtensions` directly:

```csharp
using Buttr.Unity.Injection;
using UnityEngine.SceneManagement;

// Accepts a MonoBehaviour (typed as object to avoid UnityEngine cast-gymnastics).
// The GameObject is inferred from the MonoBehaviour's .gameObject.
InjectionProcessorUnityExtensions.InjectGameObject(someMonoBehaviour);
InjectionProcessorUnityExtensions.InjectSelfAndChildren(rootMonoBehaviour);

// Scene-level:
InjectionProcessorUnityExtensions.InjectScene(myScene);
InjectionProcessorUnityExtensions.InjectActiveScene();
InjectionProcessorUnityExtensions.InjectAllLoadedScenes();
```

Both `InjectGameObject` and `InjectSelfAndChildren` expect a `MonoBehaviour` instance — passing a bare `GameObject` throws `InjectionException`. If you only have the GameObject, grab any MonoBehaviour on it first:

```csharp
var go = Instantiate(prefab);
InjectionProcessorUnityExtensions.InjectGameObject(go.GetComponent<MonoBehaviour>());
```

These are the same entry points the `SceneInjector` / `MonoInjector` components use internally.

## Plain C# classes

MonoBehaviours get field injection. Plain C# classes get **constructor injection** — no attributes needed:

```csharp
public sealed class ConsolePresenter {
    private readonly ConsoleModel m_Model;
    private readonly ILogger m_Logger;

    public ConsolePresenter(ConsoleModel model, ILogger logger) {
        m_Model = model;
        m_Logger = logger;
    }
}
```

Constructor parameters are resolved from the container the type is registered in. The `BUTTR004` analyzer flags parameter types that aren't registered anywhere.

## Analyzer rules you'll meet

| Rule | What it catches |
|---|---|
| `BUTTR001` | `[Inject]` on a non-`partial` MonoBehaviour |
| `BUTTR004` | Constructor parameter type isn't registered anywhere |
| `BUTTR007`/`008` | `[Inject]` field's resolution path issues |
| `BUTTR009` | Raw string literal scope key — fires on `[Inject("...")]`, `new ScopeBuilder("...")`, and `ScopeRegistry.Get("...")`. Prefer a `public const string` |
| `BUTTR011` | `[Inject]` on a non-MonoBehaviour in a context that won't run the source generator |

Full catalogue in the [Buttr.Core Analyzers doc](https://github.com/Crumpet-Labs/Buttr.Core/blob/main/Docs/Analyzers.md).

## Debug logging

Injection goes through the `Buttr.Core.ButtrLog` facade, which Buttr for Unity bridges to `UnityEngine.Debug.Log*` via the `Buttr.Unity.UnityButtrLogger` bootstrap. You'll see injection failures in the Console as Unity warnings/errors, not silent crashes.
