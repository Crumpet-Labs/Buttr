# Buttr

A dependency injection and architecture framework for Unity 6+. This README covers how to use Buttr within your project. For installation and contributing, see the [GitHub repository](https://github.com/Crumpet-Labs/Buttr.Unity).

Buttr works as a standalone DI framework — the design philosophy below is recommended for project cohesiveness but is not required. You can adopt the conventions gradually or use the container on its own.

---

## Project Structure

Buttr organises your project by feature rather than by file type. Everything lives inside `_Project/`.

```
Assets/
└── _Project/
    ├── {ProjectName}.asmdef     # Assembly definition for the project
    ├── Main.unity               # Boot scene — your application entry point
    ├── Program.cs               # Application composition entry point
    ├── README.md                # This file
    ├── Core/                    # Game-agnostic packages reusable across projects
    ├── Features/                # Game-specific feature packages
    ├── Shared/                  # Assets and scripts used by both Core and Features
    ├── Catalog/                 # ScriptableObject data assets, organised by feature
```

**Core** packages are game-agnostic — logging, event systems, save systems, audio management. They form a reusable library that travels with you across projects and rarely changes. **Features** are game-specific — inventory, combat, dialogue, crafting. They're built for the game you're working on right now.

Features contain only code and feature-specific resources. **All ScriptableObject assets** live in `Catalog/`, organised by feature — mirroring the structure of `Core/` and `Features/`. This includes Configurations, Definitions, and Handlers. The C# scripts that define these ScriptableObjects live in the feature; the instantiated `.asset` files live in Catalog. Loader assets are the exception — they live in the package's `Loaders/` folder alongside the Loader script.

---

## Design Philosophy

If you're coming from a traditional Unity project, some of this will feel different. No more `Scripts/` folder with hundreds of files. No more MonoBehaviours doing everything. No more guessing what a class does from its name.

Buttr replaces all of that with a simple rule: every class suffix tells you exactly what it does. Once you learn the conventions, you can open any feature in any project and immediately understand the architecture — without reading a single line of implementation.

It takes a bit of adjustment, but it's worth it.

**A note on scope:** Buttr handles feature-level architecture — singleton services, presenters, mediators, models, and registries that live once per feature. Per-instance entity management (hundreds of enemies, projectiles, or spawned objects each with their own state) is a different domain that lives within the System and Controller layer, using whatever approach fits your project's performance needs. Buttr provides the structure those systems plug into, but doesn't prescribe how individual instances are managed.

### General Rules

**Seal your classes.** All classes should be `sealed` unless they are explicitly designed for inheritance. This is standard good practice in C# — it communicates intent, prevents unintended inheritance, and allows the compiler to optimise. If a class isn't `sealed`, it should be `abstract`. If it's neither, that's a design decision that needs justifying.

**Minimise MonoBehaviours.** If something doesn't need Unity lifecycle hooks or `GetComponent<T>` access, it should be a plain C# class that gets injected. Don't make something a MonoBehaviour just because it's convenient — make it a MonoBehaviour because it needs to be on a GameObject. This keeps your GameObjects lean and your code testable.

**No empty folders.** Only create folders that are needed. Folders should only exist if they contain files.

**Package and README at the root.** Only the package file and an optional README live at the feature root — everything else is logically grouped into folders.

### Quick Reference

| Layer | Suffix | What it does | Type |
|-------|--------|-------------|------|
| **Unity** | Controller | Coordinates Unity components and systems on a GameObject | MonoBehaviour |
| | Instance | Manages a scoped container's lifecycle on a GameObject (e.g. UI panels) | MonoBehaviour |
| **Data** | Model | Data — no behaviour, no dependencies | Class / Struct |
| | Id | Readonly struct for domain-driven identity | Struct |
| | Definition | ScriptableObject entry point into a feature | ScriptableObject |
| | Configuration | ScriptableObject providing editable settings | ScriptableObject |
| **Logic** | View | Observes and displays Model data — reads only | Class |
| | Presenter | Explicit operations on Models — the only type that mutates state | Class |
| | System | Reads Model state and executes it continuously | Class |
| | Mediator | Listens to events, filters and routes towards Presenters | Class |
| | Handler | Stateless ScriptableObject — designer-facing, editor-swappable logic | ScriptableObject |
| | Behaviour | Stateful strategy — code-facing, drives a System's update loop | Class |
| **Infrastructure** | Service | Public API of a feature — the entry point other features inject | Class |
| | Repository | CRUD operations on local persistent storage | Interface |
| | Registry | Tracks active runtime objects by ID | Class |
| | Loader | ScriptableObject that bootstraps a feature at boot time | ScriptableObject |
| **Structure** | Extensions | Internal stateless functional methods that keep classes lean | Static Class |
| | Contract | Interface defining the public API boundary of a feature | Interface |

---

### Unity Layer

These are the types that touch GameObjects and live in Unity's lifecycle. All MonoBehaviours live in the `MonoBehaviours/` folder within a feature — no exceptions. Feature consumers should never have to hunt for something to drag onto a GameObject.

**Controllers** coordinate Unity components, injected services, and event systems on a GameObject. They manage lifecycle (`OnEnable`, `OnDisable`), own subscriptions, and expose a public API for other systems to interact with. Controllers are always MonoBehaviours. Controllers do not mutate feature-level Model state directly — they route actions to Presenters or raise events that Mediators handle.

**Instances** are MonoBehaviours that own a scoped container's lifecycle on a GameObject. They build a `ScopeBuilder`, inject ScriptableObjects via `ScriptableInjector`, register the feature's package, and dispose the container on `OnDestroy`. Instances are used when a feature needs its own isolated scope tied to a specific GameObject — most commonly for UI panels backed by UI Toolkit. The naming of this convention is subject to change in a future version.

---

### Data Layer

These types hold or describe data. They have no behaviour, no dependencies, and no awareness of the systems that use them.

**Models** are plain data — state with no logic. A Model doesn't know about Unity, injection, or anything else. This makes Models trivially serialisable, testable, and portable across features.

**IDs** are readonly structs that provide domain-driven identity — `EntityId` rather than `int`, `ItemId` rather than `string`. This prevents accidental cross-domain comparisons (passing a player ID where an item ID is expected) and makes APIs self-documenting. Implement `IEquatable<T>` for performance. IDs live in an `Identifiers/` folder within the feature.

**Definitions** are ScriptableObjects that serve as extensible entry points into features. Where you might traditionally use an enum (`WeaponType.Sword`), a Definition (`SwordDefinition.asset`) lets designers create new entries without touching code. Definitions describe *what* something is. The script lives in the feature; the asset lives in `Catalog/`.

**Configurations** are ScriptableObjects that provide editable settings for packages and features. They are typically injected into the application container via the `ScriptableInjector`, allowing designers to tune behaviour without touching code. The script lives in the feature; the asset lives in `Catalog/`.

---

### Logic Layer

These types make decisions. They contain the rules, strategies, and coordination logic that drive your features.

**Views** observe and display Model data but never write to it. They are plain C# classes that receive their dependencies via constructor injection. For UI Toolkit features, a View takes a `UIDocument` reference and queries visual elements from it. All mutations flow back through a Presenter — Views never write to the Model.

```csharp
public sealed class InventoryView {
    private readonly VisualElement m_Root;

    public InventoryView(UIDocument document) {
        m_Root = document.rootVisualElement;
    }
}
```

**Presenters** are plain C# classes that perform explicit operations on Models — they are the only type that should mutate Model state. They receive dependencies via constructor injection and live in `Components/` alongside other injected types.

```csharp
public sealed class ConsolePresenter {
    private readonly ConsoleModel m_Model;

    public ConsolePresenter(ConsoleModel model) {
        m_Model = model;
    }

    public void Log(ConsoleCategory category, string message)
        => m_Model.AddLog(new ConsoleLog(category, DateTime.Now, message));
}
```

**Systems** read Model state and execute it continuously. Where a View *renders* the Model visually, a System *acts* on it mechanically — applying movement, ticking Behaviours, running simulation logic. Systems own the update loop and are responsible for switching and ticking the active Behaviour. They are typically plain C# classes injected into the MonoBehaviour that hosts them, keeping the GameObject lean.

```csharp
public sealed class MovementSystem {
    private IMovementBehaviour m_ActiveBehaviour;

    public IMovementBehaviour ActiveBehaviour {
        get => m_ActiveBehaviour;
        set => m_ActiveBehaviour = value;
    }

    public void Tick(MovementContext ctx) {
        m_ActiveBehaviour?.Tick(ctx);
    }
}
```

**Mediators** are self-contained event listeners. They bind to events in their constructor, apply filtering or transformation logic, and route results to the relevant Presenter — or exit early if the event isn't applicable. Nothing calls into a Mediator from the outside; they react to events implicitly. Mediators unbind from events on disposal. They are injectable plain C# classes that live in `Components/`, and they use standard C# events or event buses — not UnityEvents.

The standard Mediator pattern takes a Presenter, Model, and View — observing Model changes and updating the View accordingly. What gets injected depends on the feature's needs; there's no enforced rule beyond the convention that Mediators react to changes rather than being called into directly.

```csharp
public sealed class InventoryMediator : IDisposable {
    private readonly InventoryPresenter m_Presenter;
    private readonly InventoryModel m_Model;
    private readonly InventoryView m_View;

    public InventoryMediator(InventoryPresenter presenter, InventoryModel model, InventoryView view) {
        m_Presenter = presenter;
        m_Model = model;
        m_View = view;

        // Subscribe to model changes, update view accordingly
    }

    public void Dispose() {
        // Unsubscribe from model changes
    }
}
```

**Handlers** are abstract ScriptableObjects that provide a **feature-specific public API** for strategic logic. They are **stateless** and **designer-facing** — a designer drags a different Handler asset onto a component and behaviour changes without a recompile. Handlers pair with `DIBuilder<TKey>` for runtime resolution by key. Think of Definitions as the "what" and Handlers as the "how." The script lives in a `Handlers/` folder within the feature; the asset lives in `Catalog/`.

**Behaviours** are lightweight strategy objects that provide a **feature-specific Tick method** to drive a System's update loop. They are **stateful** and **code-facing** — constructed with a Model reference, switched at runtime through code, and ticked by their host System. Only the active Behaviour is ticked each frame. Behaviours live in a `Behaviours/` folder within the feature.

```csharp
public interface IMovementBehaviour {
    void Tick(MovementContext ctx);
}

public sealed class SprintBehaviour : IMovementBehaviour {
    public void Tick(MovementContext ctx) {
        ctx.Velocity = ctx.Direction * ctx.SprintSpeed;
    }
}
```

| | Handler | Behaviour |
|---|---------|-----------|
| **State** | Stateless | Stateful — constructed with Model |
| **Audience** | Designer-facing — swappable in editor | Code-facing — switched at runtime |
| **Type** | ScriptableObject | Plain C# class |
| **API** | Feature-specific public methods | Feature-specific Tick method |
| **Resolution** | `DIBuilder<TKey>` by key | Owned and ticked by host System |

---

### Infrastructure Layer

These types connect your features to the outside world — APIs, persistence, and runtime state tracking. All infrastructure types are injected and live in `Components/`.

**Services** are the public API of a feature — the entry point that other features inject to interact with this feature's capabilities. Some Services communicate with external APIs, asset systems, or remote databases. Others wrap Registries and internal logic behind a clean interface. The commonality is that a Service is the boundary — the front door that other features knock on. Services pair with Contracts to define their public interface.

```csharp
public sealed class StatsService : IStatsService {
    private readonly StatsRegistry m_Registry;

    public StatsService(StatsRegistry registry) {
        m_Registry = registry;
    }

    public float Get(EntityId entityId, StatDefinition definition)
        => m_Registry.Get(entityId).Get(definition);

    public void AddModifier(EntityId entityId, in StatModifier modifier)
        => m_Registry.Get(entityId).Modifiers.Add(modifier);
}
```

**Repositories** are interfaces that define CRUD operations on local persistent storage. They own the persistence boundary and expose domain-friendly methods rather than raw storage calls. The pattern is the same regardless of storage engine; only the implementation changes. Repository contracts live in `Contracts/` alongside Service contracts.

**Registries** are the runtime counterpart to Repositories. Where a Repository persists data to storage, a Registry tracks what's alive and accessible right now — active entities, spawned objects, loaded resources — typically keyed by `EntityId`. Registration returns a disposable handle for automatic deregistration on disposal.

**Loaders** are `UnityApplicationLoaderBase` ScriptableObjects that bootstrap a feature at boot time. They register dependencies, initialise systems, and wire up the feature's package. Loaders are the bridge between the boot pipeline and your feature's DI configuration. The script lives in a `Loaders/` folder within the feature; the asset also lives in the package's `Loaders/` folder.

---

### Structure

**Extensions** are a core pattern in Buttr. Internal extension methods are preferred over private class methods to keep classes focused. Extensions should be functional — they take input, return a value, and don't require access to private state. If a method needs many values to function, that's a signal that something structural needs rethinking. Private methods are still appropriate when genuinely justified, but reaching for an extension first is the default.

```csharp
internal static class InventoryExtensions {
    public static bool TryStack(this InventoryModel inventory, ItemId id, int amount) {
        var slot = inventory.FindSlot(id);
        if (slot == null || slot.Count + amount > slot.MaxStack) return false;
        slot.Count += amount;
        return true;
    }
}
```

Extensions also serve as the mechanism for Configurable Packages — each package exposes a `builder.UseSomething()` extension method that encapsulates its registrations.

**Contracts** are interfaces that define the public API boundary of a feature. The folder name reflects their purpose — an interface is a contract between a feature and its consumers. Contracts live in a `Contracts/` folder within the feature.

---

### Feature Structure

Every feature follows the same layout. Only the package file and an optional README live at the feature root — everything else is logically grouped into folders.

```
Features/
└── Console/
    ├── ConsolePackage.cs           # Package entry point — always at root
    ├── {Namespace}.asmdef          # Assembly definition for this package
    ├── README.md                   # Feature documentation — optional
    ├── Components/                 # Injected types: Presenters, Models,
    │   ├── ConsolePresenter.cs     #   Services, Repositories, Registries,
    │   ├── ConsoleModel.cs         #   Mediators, Systems, Views
    │   ├── ConsoleView.cs
    │   └── ConsoleMediator.cs
    ├── Configurations/             # Configuration ScriptableObject scripts
    │   └── ConsoleConfiguration.cs
    ├── Contracts/                  # Interfaces / public API boundaries
    │   └── IConsoleService.cs
    ├── Identifiers/                # Readonly ID structs
    │   └── ConsoleLogId.cs
    ├── MonoBehaviours/             # ALL MonoBehaviours for this feature
    │   └── ConsoleController.cs
    ├── Common/                     # Supporting types that don't fit elsewhere
    │   ├── ConsoleLog.cs
    │   └── ConsoleCategory.cs
    ├── Loaders/                    # Boot-time loader scripts and assets
    │   ├── ConsoleLoader.cs
    │   └── ConsoleLoader.asset
    └── Exceptions/                 # Custom exceptions — only if necessary
        └── ConsoleException.cs
```

A more complex feature with Handlers and Behaviours:

```
Features/
└── Combat/
    ├── CombatPackage.cs
    ├── {Namespace}.asmdef
    ├── README.md
    ├── Components/
    │   ├── CombatPresenter.cs
    │   ├── CombatModel.cs
    │   ├── CombatView.cs
    │   ├── CombatSystem.cs
    │   ├── DamageMediator.cs
    │   ├── StatsService.cs
    │   └── CombatRegistry.cs
    ├── Contracts/
    │   └── IStatsService.cs
    ├── Identifiers/
    │   ├── CombatantId.cs
    │   └── AbilityId.cs
    ├── Handlers/
    │   ├── MeleeAttackHandler.cs
    │   └── RangedAttackHandler.cs
    ├── Behaviours/
    │   ├── AggressiveBehaviour.cs
    │   └── DefensiveBehaviour.cs
    ├── MonoBehaviours/
    │   ├── CombatAnimationController.cs
    │   └── CombatInstance.cs
    ├── Common/
    │   ├── DamageEvent.cs
    │   ├── CombatContext.cs
    │   └── CombatConstants.cs
    └── Loaders/
        ├── CombatLoader.cs
        └── CombatLoader.asset
```

A UI feature using UI Toolkit:

```
Features/
└── Inventory/
    ├── InventoryPackage.cs
    ├── {Namespace}.asmdef
    ├── README.md
    ├── Components/
    │   ├── InventoryPresenter.cs
    │   ├── InventoryModel.cs
    │   ├── InventoryView.cs
    │   ├── InventoryMediator.cs
    │   └── InventoryService.cs
    ├── Contracts/
    │   └── IInventoryService.cs
    ├── MonoBehaviours/
    │   └── InventoryInstance.cs
    └── Common/
        └── InventorySlot.cs
```

The `Catalog/` folder mirrors the feature structure for ScriptableObject data assets (Configurations, Definitions, and Handlers). Loader assets are the exception — they live in the package's `Loaders/` folder:

```
Catalog/
├── Console/
│   └── ConsoleConfiguration.asset
└── Combat/
    ├── CombatConfiguration.asset
    ├── Definitions/
    │   ├── MeleeWeaponDefinition.asset
    │   └── RangedWeaponDefinition.asset
    └── Handlers/
        ├── MeleeAttackHandler.asset
        └── RangedAttackHandler.asset
```

---

## Setup Wizard

After installing Buttr via UPM, the setup wizard appears automatically. It offers two paths:

- **Quick Setup** — Accepts all convention defaults. Scaffolds `_Project/`, creates the boot scene, generates `Program.cs`, `ProgramLoader`, and an assembly definition, and configures build settings. One click and you're ready to build.
- **Skip Conventions** — Installs Buttr as a standalone DI framework with no folder structure or scaffolding. Use the container on its own and adopt conventions later if you choose.

You can re-run the wizard at any time from `Tools > Buttr > Setup Project`.

## Editor Tooling

Buttr provides right-click context menus in the Project window to scaffold packages and types that follow the conventions. These are available under `Right-Click > Buttr > Packages`.

### Create Package

**New Feature**, **New Core Package**, and **New UI Package** prompt for a name and scaffold the full package structure — the package entry point, assembly definition, Components folder with a Model, Presenter, Mediator, View, and Service, Contracts folder with the service interface, and a Loader (or Instance for UI packages). All classes are correctly named, sealed, and wired with constructor injection. Optional additions (Handlers, Behaviours, Identifiers, Configurations, Common, Exceptions) can be selected during creation.

UI packages scaffold a `{Name}Instance` MonoBehaviour that owns a `ScopeBuilder` and `UIDocument` reference, and a `{Name}View` class that takes the `UIDocument` for querying visual elements.

### Add to Package

**Add to Package** lets you add individual types to an existing package. It infers the package name from the folder you right-click in, creates the appropriate subfolder if it doesn't exist, and drops in a correctly templated file. Types are grouped by their architectural layer:

- **Unity** — Controller, View, Instance
- **Data** — Model, Identifier, Definition, Configuration
- **Logic** — Presenter, System, Mediator, Handler, Behaviour
- **Infrastructure** — Service + Contract (scaffolds both together), Repository, Registry, Loader
- **Structure** — Extensions

## Boot System

`Main.unity` is your application's entry point. It contains a single `Boot` GameObject with the `UnityApplicationBoot` component, which loads `UnityApplicationLoaderBase` ScriptableObjects in sequence using async/await.

### Program.cs

Buttr's setup wizard generates a `Program.cs` file — a familiar convention from .NET frameworks. It separates application composition (which packages to register) from Unity's boot lifecycle, keeping your entry point pure C# and testable.

By moving application composition into a standard C# entry point, you can see your entire game's dependency structure in a single file. This prevents dependencies from being scattered across scene hierarchies and makes your architecture testable without the Unity Editor.

Buttr also provides `CMDArgs`, a static utility that parses command line arguments before anything else runs (via `RuntimeInitializeOnLoadMethod`). This makes launch arguments available to `Program.Main()` for build configurations, server flags, debug modes, and similar use cases.

```csharp
public static class Program {
    public static ApplicationContainer Main() => Main(CMDArgs.Read());

    private static ApplicationContainer Main(IDictionary<string, string> args) {
        var builder = new ApplicationBuilder();

        builder.UseConsole();
        builder.UseAudio();
        builder.UseNetworking();

        return builder.Build();
    }
}
```

The wizard also generates a `ProgramLoader` — a thin `UnityApplicationLoaderBase` that calls `Program.Main()` and manages the container:

```csharp
[CreateAssetMenu(fileName = "ProgramLoader", menuName = "Buttr/Loaders/Program", order = 0)]
public sealed class ProgramLoader : UnityApplicationLoaderBase {
    private ApplicationContainer m_Container;

    public override Awaitable LoadAsync(CancellationToken cancellationToken) {
        m_Container = Program.Main();
        return AwaitableUtility.CompletedTask;
    }

    public override Awaitable UnloadAsync() {
        m_Container?.Dispose();
        return AwaitableUtility.CompletedTask;
    }
}
```

All composition lives in `Program.cs`. The Loader is just the bridge to Unity's lifecycle. Both are generated by the setup wizard.

### Custom Loaders

For features that need their own boot-time setup beyond the main `Program.cs`, create additional loaders by inheriting from `UnityApplicationLoaderBase`:

```csharp
public sealed class SceneLoader : UnityApplicationLoaderBase {
    private ApplicationContainer m_Container;

    public override Awaitable LoadAsync(CancellationToken cancellationToken) {
        var builder = new ApplicationBuilder();

        builder.Resolvers.AddSingleton<ISceneService, SceneService>();

        m_Container = builder.Build();
        return AwaitableUtility.CompletedTask;
    }

    public override Awaitable UnloadAsync() {
        m_Container?.Dispose();
        return AwaitableUtility.CompletedTask;
    }
}
```

The boot scene handles initial application setup. After bootstrapping, scene management is your responsibility — load and unload scenes through your own Core features (Addressables, SceneManager, or whatever fits your project). Buttr provides the patterns (Loaders, Services, Scopes) but doesn't prescribe how scenes are managed after boot.

## Application Container

`ApplicationBuilder` registers dependencies to a static resolver, accessible anywhere via `Application.Get<T>()`. Dependencies are lazily instantiated — they're only allocated when first requested.

> **In practice, you should rarely call `Application.Get<T>()` directly.** It exists primarily for the source generator and as a migration path for developers moving away from MonoSingleton patterns. In normal usage, dependencies should be resolved through `[Inject]` on MonoBehaviours or constructor injection on plain C# classes.

```csharp
var builder = new ApplicationBuilder();

// Singletons — one instance, shared everywhere
builder.Resolvers.AddSingleton<ISingletonFoo, SingletonFoo>();
builder.Resolvers.AddSingleton<SingletonBar>();

// Transients — new instance each time
builder.Resolvers.AddTransient<ITransientFoo, TransientFoo>();
builder.Resolvers.AddTransient<TransientBar>();

// Hidden — available for constructor injection, but not via Application.Get<T>()
builder.Hidden.AddSingleton<ISingletonHidden, SingletonHidden>();

// Factories and post-build configuration
builder.Resolvers.AddTransient<TestConfigurable>()
    .WithFactory(() => new TestConfigurable())
    .WithConfiguration(obj => { obj.Foo = "Bar"; return obj; });

var app = builder.Build();

// Resolve from anywhere
var foo = Application.Get<ITransientFoo>();
var bar = Application.Get<SingletonBar>();

// Dispose cleans up all resolved IDisposables
app.Dispose();
```

All Buttr containers — application, scope, and standalone — automatically dispose any registered type that implements `IDisposable` when the container itself is disposed. The pattern is: a Loader builds the container, stores the returned container reference, and disposes it in `UnloadAsync`. This ensures Mediators, Services, and any other type with cleanup logic are handled without manual disposal.

## Scoped Containers

Scopes provide isolated dependency containers for feature-specific dependencies that shouldn't live at the application level. Scopes check their own container first, then fall back to the application container.

Define scope keys as constants to avoid magic strings and enable refactoring across the project:

```csharp
public static class Scopes {
    public const string Inventory = "inventory";
    public const string Gameplay = "gameplay";
}
```

```csharp
var builder = new ScopeBuilder(Scopes.Inventory);

builder.Resolvers.AddTransient<IFoo, Foo>();
builder.Resolvers.AddSingleton<Bar>();

var container = builder.Build();

// Access directly
var foo = container.Get<IFoo>();

// Or from anywhere via the registry
var sameContainer = ScopeRegistry.Get(Scopes.Inventory);

// Dispose removes from registry and cleans up all resolved IDisposables
container.Dispose();
```

Use scopes for dependencies that should live shorter than the application — scene-specific systems, feature instances that come and go, or isolated subsystems that need their own lifecycle.

## MonoBehaviour Injection

**Buttr generates injection code at compile time — zero runtime reflection.** MonoBehaviours using injection **must** be `partial`.

```csharp
public partial class CombatAnimationController : MonoBehaviour {
    [Inject] private IInputService _inputService;
    [Inject(Scopes.Gameplay)] private ICombatSystem _combatSystem;
}
```

Use one of the provided injector components to resolve at runtime:

- **`SceneInjector`** — Resolves all `[Inject]` dependencies for every MonoBehaviour in the scene before `Awake`.
- **`MonoInjector`** — Resolves all `[Inject]` dependencies for MonoBehaviours on a specific GameObject before `Awake`.

## DIBuilder

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

A specialised container for strategy pattern resolution, where objects are registered and retrieved by key:

```csharp
var builder = new DIBuilder<string>();
builder.AddSingleton<Foo>("foo");
builder.AddSingleton<Bar>("bar");

var container = builder.Build();
var foo = container.Get("foo");
```

## Configurable Packages

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

## Roslyn Analyzers

Buttr includes Roslyn analyzers that catch common DI mistakes at compile time. These run automatically in your IDE and Unity console, flagging issues such as using `[Inject]` on a non-partial MonoBehaviour, missing dependency registrations, incorrect attribute usage, and structural issues in your DI setup.

## Source Generation

**Source generators run at compile time to produce injection code for any MonoBehaviour using `[Inject]` or `[Inject("scope")]`. There is zero runtime reflection overhead.** Source generation is always active and is a core part of how Buttr operates.

## Requirements

- Unity 6 or later
- .NET Standard 2.1 / .NET 6+