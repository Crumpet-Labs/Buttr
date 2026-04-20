# Buttr for Unity

A lightweight dependency-injection container with an opinionated architecture for Unity 6+. Vendors the engine-agnostic [Buttr.Core](https://github.com/Crumpet-Labs/Buttr.Core) library and adds MonoBehaviour, ScriptableObject, and scene-walking integration on top.

- **Source-generated injection** — `[Inject]` fields on `partial` MonoBehaviours. Zero runtime reflection.
- **Roslyn analyzers** — compile-time diagnostics for duplicate registrations, missing dependencies, alias mismatches, and more.
- **Suffix-driven architecture** — Models, Presenters, Services, Handlers, Behaviours. Every class name tells you what it does.
- **Editor scaffolding** — one-click project setup, right-click to scaffold conventions-compliant packages.

## Installation

`Window > Package Manager` → `+` → **Install package from git URL**:

```
https://github.com/Crumpet-Labs/Buttr.Unity.git?path=Assets/Plugins/Buttr
```

Pin a version by appending `#v2.3.0`. Requires Unity 6.0+.

## Getting started

1. `Tools > Buttr > Setup Project` — scaffolds `_Project/`, `Main.unity` boot scene, `Program.cs`, `ProgramLoader`.
2. Open `Program.cs`, register a service:

   ```csharp
   public static class Program {
       public static ApplicationContainer Main() => Main(CMDArgs.Read());

       private static ApplicationContainer Main(IDictionary<string, string> args) {
           var builder = new ApplicationBuilder();
           builder.Resolvers.AddSingleton<IGreeter, Greeter>();
           return builder.Build();
       }
   }
   ```

3. Inject it into a `partial` MonoBehaviour:

   ```csharp
   public partial class Welcome : MonoBehaviour {
       [Inject] private IGreeter m_Greeter;

       private void Start() => Debug.Log(m_Greeter.Greet("world"));
   }
   ```

4. Add a `SceneInjector` to your scene, press Play.

Full walkthrough: [Docs/Guides/GettingStarted.md](https://github.com/Crumpet-Labs/Buttr.Unity/blob/main/Docs/Guides/GettingStarted.md).

## Documentation

Unity-specific guides live in [Docs/Guides/](https://github.com/Crumpet-Labs/Buttr.Unity/tree/main/Docs/Guides):

| Guide | What it covers |
|---|---|
| [Installation](https://github.com/Crumpet-Labs/Buttr.Unity/blob/main/Docs/Guides/Installation.md) | UPM install, updating, version pinning |
| [Getting Started](https://github.com/Crumpet-Labs/Buttr.Unity/blob/main/Docs/Guides/GettingStarted.md) | First project — `Program.cs`, `ProgramLoader`, boot scene |
| [Conventions](https://github.com/Crumpet-Labs/Buttr.Unity/blob/main/Docs/Guides/Conventions.md) | Suffix-driven architecture |
| [MonoBehaviour Injection](https://github.com/Crumpet-Labs/Buttr.Unity/blob/main/Docs/Guides/MonoBehaviourInjection.md) | `[Inject]`, `partial`, `SceneInjector`, `MonoInjector` |
| [ScriptableObjects](https://github.com/Crumpet-Labs/Buttr.Unity/blob/main/Docs/Guides/ScriptableObjects.md) | `ScriptableInjector`, Configurations, Definitions, Handlers |
| [Editor Tooling](https://github.com/Crumpet-Labs/Buttr.Unity/blob/main/Docs/Guides/EditorTooling.md) | Setup project, right-click scaffolding |
| [Loaders](https://github.com/Crumpet-Labs/Buttr.Unity/blob/main/Docs/Guides/Loaders.md) | `UnityApplicationBoot`, boot pipeline |

Engine-agnostic core (aliasing, `All<T>()`, `DIBuilder`, `Hidden`, analyzer catalogue): [Buttr.Core docs](https://github.com/Crumpet-Labs/Buttr.Core/tree/main/Docs).

## License

MIT — see [LICENSE.md](LICENSE.md).
