<p align="center">
  <!-- Replace with actual logo path -->
  <img src="Docs/Images/buttr-wordmark.svg" alt="Buttr Logo" width="400"/>
</p>

<p align="center">
  <strong>A lightweight Dependency Injection framework built in pure C# — currently targeting Unity 6+</strong>
</p>

<p align="center">
  <a href="https://github.com/Crumpet-Labs/Buttr.Unity/releases"><img src="https://img.shields.io/github/v/release/Crumpet-Labs/Buttr.Unity?style=flat-square" alt="Release"></a>
  <a href="https://github.com/Crumpet-Labs/Buttr.Unity/blob/main/LICENSE"><img src="https://img.shields.io/github/license/Crumpet-Labs/Buttr.Unity?style=flat-square" alt="License"></a>
  <a href="https://unity.com"><img src="https://img.shields.io/badge/Unity-6+-black?style=flat-square&logo=unity" alt="Unity 6+"></a>
  <a href="https://learn.microsoft.com/en-us/dotnet/csharp/"><img src="https://img.shields.io/badge/C%23-9-blue?style=flat-square&logo=csharp" alt="C#"></a>
</p>

<p align="center">
  <a href="#installation">Installation</a> •
  <a href="#getting-started">Getting Started</a> •
  <a href="#features">Features</a> •
  <a href="#documentation">Documentation</a> •
  <a href="#contributing">Contributing</a>
</p>

---

Buttr uses lazy instantiation and expression trees at its core, minimising reflection through compile-time source generation for attribute injection. It's designed to be lightning fast at resolving dependencies and is extendable to allow for reusable, configurable packages.

It's small by design, built to fit into existing `MonoSingleton` architectures with minimal changes — making it easy to adopt without learning a large toolset or changing how your team thinks about code.

> [!WARNING]
> Buttr has not yet been tested on WebGL, or consoles. Additional testing is recommended for those platforms.

## Features

- **Source Generation** — `[Inject]` and `[Inject("scope")]` attributes generate injection code at compile time. No runtime reflection.
- **Roslyn Analyzers** — Catch common DI mistakes before you hit play. Missing registrations, incorrect attribute usage, and structural issues are flagged as compiler warnings and errors.
- **Setup Wizard** — Install via UPM, open Unity, and the setup wizard scaffolds your project structure automatically.
- **Editor Tooling** — Right-click context menus scaffold packages and individual types that follow the conventions.
- **Application Containers** — Build application-wide dependencies with `ApplicationBuilder` and access them anywhere via `Application.Get<T>()`.
- **Scoped Containers** — Create isolated dependency scopes with `ScopeBuilder` for feature-specific resolution.
- **Configurable Packages** — Bundle reusable DI configurations with `ConfigurableCollection` for plug-and-play package development.
- **Async Boot System** — `UnityApplicationBoot` and `UnityApplicationLoaderBase` provide a clean async/await loading pipeline.

## Installation

### Unity Package Manager (Recommended)

1. Open Unity
2. Open the Package Manager (`Window > Package Manager`)
3. Click the `+` button in the top left
4. Select **Install package from git URL**
5. Paste the following URL:

```
https://github.com/Crumpet-Labs/Buttr.Unity.git?path=Assets/Plugins/Buttr
```

### Releases

Download the latest `.unitypackage` from the [Releases](https://github.com/Crumpet-Labs/Buttr.Unity/releases) page.

## Getting Started

### Setup Wizard

After installing Buttr, the setup wizard will appear automatically when you open Unity.

<!-- Replace with actual screenshot -->
<p align="center">
  <img src="docs/images/setup-wizard.png" alt="Buttr Setup Wizard" width="450"/>
</p>

The wizard displays your project name, Unity version, and Buttr version. Click **Quick Setup** and Buttr will scaffold the following structure in your `Assets` folder:

```
Assets/
└── _Project/
    ├── {ProjectName}.asmdef  # Assembly definition for the project
    ├── Main.unity            # Boot scene (added to build settings at index 0)
    ├── Program.cs            # Application composition entry point
    ├── README.md             # Usage documentation
    ├── Core/                 # Core packages used by features
    ├── Features/             # Feature-specific packages
    ├── Shared/               # Assets and scripts shared across Core and Features
    └── Catalog/              # ScriptableObject data assets, organised by feature
```

You can re-run the wizard at any time from `Tools > Buttr > Setup Project`.

### Boot Scene

The generated `Main.unity` scene contains a single `Boot` GameObject with the `UnityApplicationBoot` component. This is your application's entry point.

`UnityApplicationBoot` loads `UnityApplicationLoaderBase` ScriptableObjects in sequence, providing a clean async/await pipeline for bootstrapping your application.

```csharp
public sealed class ApplicationLoader : UnityApplicationLoaderBase {
    private ApplicationContainer m_Container;

    public override Awaitable LoadAsync(CancellationToken cancellationToken) {
        var builder = new ApplicationBuilder();

        builder.Resolvers.AddSingleton<IGameService, GameService>();
        builder.Resolvers.AddSingleton<IAudioManager, AudioManager>();

        m_Container = builder.Build();
        return AwaitableUtility.CompletedTask;
    }

    public override Awaitable UnloadAsync() {
        m_Container?.Dispose();
        return AwaitableUtility.CompletedTask;
    }
}
```

### Application Container

The `ApplicationBuilder` registers dependencies to a static resolver registry, accessible anywhere via `Application.Get<T>()`. Dependencies are lazily instantiated — they're only allocated when first requested.

```csharp
var builder = new ApplicationBuilder();

// Singletons resolve to a single instance
builder.Resolvers.AddSingleton<ISingletonFoo, SingletonFoo>();
builder.Resolvers.AddSingleton<SingletonBar>();

// Transients resolve to a new instance each time
builder.Resolvers.AddTransient<ITransientFoo, TransientFoo>();
builder.Resolvers.AddTransient<TransientBar>();

// Hidden objects are available for constructor injection but not via Application.Get<T>()
builder.Hidden.AddSingleton<ISingletonHidden, SingletonHidden>();

// Configure factories and post-build configuration
builder.Resolvers.AddTransient<TestConfigurable>()
    .WithFactory(() => new TestConfigurable())
    .WithConfiguration(obj => { obj.Foo = "Bar"; return obj; });

// Build returns a disposable container
var app = builder.Build();

// Access dependencies anywhere
var foo = Application.Get<ITransientFoo>();
var bar = Application.Get<SingletonBar>();

// Dispose to clean up — resolved singletons are disposed, transients are not
app.Dispose();
```

### Scoped Containers

Scopes provide isolated dependency containers, useful for feature-specific dependencies that shouldn't live at the application level.

```csharp
var builder = new ScopeBuilder("inventory");

builder.Resolvers.AddTransient<IFoo, Foo>();
builder.Resolvers.AddSingleton<Bar>();

// Building registers the container with ScopeRegistry
// Scopes check their own container first, then fall back to the application container
var container = builder.Build();

// Access directly
var foo = container.Get<IFoo>();

// Or from anywhere via the registry
var sameContainer = ScopeRegistry.Get("inventory");

// Dispose removes from registry and disposes resolved singletons
container.Dispose();
```

### MonoBehaviour Injection

Buttr generates injection code at compile time — no runtime reflection. MonoBehaviours using injection must be `partial`.

```csharp
public partial class PlayerController : MonoBehaviour {
    [Inject] private IInputService _inputService;
    [Inject("gameplay")] private ICombatSystem _combatSystem;
}
```

To inject at runtime, use one of the provided injector components:

- **`SceneInjector`** — Resolves all `[Inject]` dependencies for every MonoBehaviour in the scene before `Awake` is called.
- **`MonoInjector`** — Resolves all `[Inject]` dependencies for MonoBehaviours on a specific GameObject before `Awake` is called.

### Source Generation

Buttr's source generators run at compile time to produce the injection code for any MonoBehaviour using `[Inject]` or `[Inject("Scope")]`. This means there is zero runtime reflection overhead — all injection is handled through generated static methods.

Source generation is always active and cannot be disabled. It is a core part of how Buttr operates.

### Roslyn Analyzers

Buttr includes Roslyn analyzers that catch common mistakes at compile time. These provide warnings and errors directly in your IDE and Unity console for issues such as:

- Using `[Inject]` on a non-partial MonoBehaviour
- Missing dependency registrations
- Incorrect attribute usage
- Structural issues in your DI setup

Analyzers are bundled with the package and are always active.

### DIBuilder

For ad-hoc dependency resolution that doesn't need to be globally or scope-accessible:

```csharp
var builder = new DIBuilder();
builder.AddSingleton<IFoo, Foo>();
builder.AddTransient<Bar>();

var container = builder.Build();
var foo = container.Get<IFoo>();
```

`DIBuilder` resolves from its own container first, then falls back to the application container.

### DIBuilder&lt;TKey&gt;

A specialised container for strategy pattern resolution. Objects are registered and retrieved by key.

```csharp
var builder = new DIBuilder<string>();

builder.AddSingleton<Foo>("foo");
builder.AddSingleton<Bar>("bar");

var container = builder.Build();

var foo = container.Get("foo");
var bar = container.Get("bar");
```

This enables dynamic strategy resolution at runtime — useful for scenarios like loading behaviour from data-driven configurations.

### Configurable Packages

Bundle reusable DI registrations into packages using `ConfigurableCollection`:

```csharp
public static IConfigurableCollection UseNetworking(this ApplicationBuilder builder) {
    return new ConfigurableCollection()
        .Register(builder.Resolvers.AddSingleton<INetworkClient, NetworkClient>())
        .Register(builder.Resolvers.AddSingleton<ISessionManager, SessionManager>());
}

// Usage
var builder = new ApplicationBuilder();
builder.UseNetworking()
    .WithConfiguration<NetworkClient>(client => {
        client.Endpoint = "https://api.example.com";
        return client;
    });

builder.Build();
```

Configurable collections work with `ApplicationBuilder`, `ScopeBuilder`, and `DIBuilder`.

## Project Structure

Buttr enforces a feature-based project structure rather than the traditional Unity folder organisation (Scripts, Scenes, Prefabs, etc.).

| Folder | Purpose |
|--------|---------|
| `Core/` | Core packages that are shared across features |
| `Features/` | Self-contained feature packages |
| `Shared/` | Assets and scripts used by both Core and Features |
| `Catalog/` | ScriptableObject data assets, organised by feature |

## Requirements

- Unity 6 or later
- .NET Standard 2.1 / .NET 6+

## Roadmap

- [ ] WebGL, and console platform testing
- [ ] Additional Roslyn analyzer rules
- [ ] Editor tooling improvements

## Contributing

Contributions are welcome. Please open an issue first to discuss what you'd like to change.

## License

This project is licensed under the [MIT License](LICENSE).