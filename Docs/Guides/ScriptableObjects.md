# ScriptableObjects

ScriptableObjects are first-class in Buttr's architecture — they're how designer-facing data and strategy enter the DI graph. This guide covers the three idiomatic ScriptableObject roles (Configuration, Definition, Handler) and the `ScriptableInjector` that registers them.

For the suffix-level rules see [Conventions](Conventions.md).

## The three ScriptableObject roles

| Role | Purpose | Stateful? |
|---|---|---|
| **Configuration** | Editable settings tuned by designers | No |
| **Definition** | Extensible "what is this" entries (replaces enums) | No |
| **Handler** | Stateless, designer-swappable strategy logic | No |

All three are **stateless** — they describe, not execute. Stateful strategy belongs in a `Behaviour` (plain C# class).

## ScriptableInjector

`ScriptableInjector` is a `[Serializable]` helper class (not a MonoBehaviour) that bulk-registers ScriptableObject assets into a Buttr container. Expose it on a Loader as a `[SerializeField]`, drag your `.asset` references into its list in the Inspector, and call `Inject(builder)` from `LoadAsync`. Internally it builds an Expression-tree-compiled `AddSingleton<TConcrete>().WithFactory(() => instance)` registration for each entry, keyed by each asset's concrete type.

Typical flow inside a Loader:

```csharp
public sealed class CombatLoader : UnityApplicationLoaderBase {
    [SerializeField] private ScriptableInjector m_Injector;

    private ApplicationContainer m_Container;

    public override Awaitable LoadAsync(CancellationToken ct) {
        var builder = new ApplicationBuilder();

        m_Injector.Inject(builder);   // Registers each ScriptableObject under its concrete type
        builder.Resolvers.AddSingleton<ICombatService, CombatService>();

        m_Container = builder.Build();
        return AwaitableUtility.CompletedTask;
    }

    public override Awaitable UnloadAsync() {
        m_Container?.Dispose();
        return AwaitableUtility.CompletedTask;
    }
}
```

`ScriptableInjector` overloads `Inject(ApplicationBuilder)` and `Inject(IDIBuilder)` — same pattern works inside a `ScopeBuilder` or `DIBuilder` Loader too.

If you need fine-grained control — registering individual assets, applying configuration, or providing a different concrete type — skip `ScriptableInjector` and use `WithFactory` directly:

```csharp
[SerializeField] private CombatConfiguration m_Config;

public override Awaitable LoadAsync(CancellationToken ct) {
    var builder = new ApplicationBuilder();

    builder.Resolvers.AddSingleton<CombatConfiguration>().WithFactory(() => m_Config);

    m_Container = builder.Build();
    return AwaitableUtility.CompletedTask;
}
```

The `WithFactory` lambda short-circuits Buttr's constructor scan — perfect for ScriptableObjects since they're created by Unity, not by Buttr.

`[SerializeField]` slots on the Loader expose drag-targets in the Inspector. The `.asset` files themselves live in `Catalog/` (see [Conventions](Conventions.md)).

## Configurations

A Configuration is a ScriptableObject with editable settings that tune a package's behaviour.

```csharp
[CreateAssetMenu(fileName = "AudioConfiguration",
                 menuName = "Game/Audio/Configuration")]
public sealed class AudioConfiguration : ScriptableObject {
    [SerializeField] private float m_MasterVolume = 1.0f;
    [SerializeField] private AudioMixer m_Mixer;

    public float MasterVolume => m_MasterVolume;
    public AudioMixer Mixer => m_Mixer;
}
```

Consumers inject the concrete Configuration type directly:

```csharp
public sealed class AudioService : IAudioService {
    private readonly AudioConfiguration m_Config;

    public AudioService(AudioConfiguration config) {
        m_Config = config;
        m_Config.Mixer.SetFloat("Master", m_Config.MasterVolume);
    }
}
```

The Configuration **script** lives in the feature's `Configurations/` folder. The Configuration **asset** lives in `Catalog/{Feature}/`.

## Definitions

A Definition is an extensible enum-replacement. Instead of a `WeaponType.Sword` enum entry that requires code changes to extend, designers create new `SwordDefinition.asset` files.

```csharp
[CreateAssetMenu(fileName = "WeaponDefinition",
                 menuName = "Game/Combat/Weapon Definition")]
public sealed class WeaponDefinition : ScriptableObject {
    [SerializeField] private string m_DisplayName;
    [SerializeField] private int m_BaseDamage;
    [SerializeField] private float m_AttackSpeed;

    public string DisplayName => m_DisplayName;
    public int BaseDamage => m_BaseDamage;
    public float AttackSpeed => m_AttackSpeed;
}
```

Definitions are usually referenced directly from gameplay code (via serialised fields on MonoBehaviours, or looked up by ID through a Registry). They don't always need to be in the DI container — but if they're part of a fixed set loaded at boot, register them via `ScriptableInjector`.

## Handlers

Handlers are abstract ScriptableObjects providing a **feature-specific public API** for strategic logic. The pattern pairs well with `DIBuilder<TKey>` for designer-assignable strategies.

```csharp
public abstract class AttackHandler : ScriptableObject {
    public abstract DamageResult Execute(CombatContext ctx);
}

[CreateAssetMenu(menuName = "Game/Combat/Handlers/Melee Attack")]
public sealed class MeleeAttackHandler : AttackHandler {
    [SerializeField] private int m_Damage = 10;

    public override DamageResult Execute(CombatContext ctx) {
        return new DamageResult(m_Damage, DamageType.Physical);
    }
}
```

Register each Handler under its **concrete** type and resolve them polymorphically via `All<T>()`. Buttr's `All<T>` walks every registration whose concrete type is assignable to `T`, so the abstract `AttackHandler` doesn't need its own registration:

```csharp
[SerializeField] private MeleeAttackHandler m_Melee;
[SerializeField] private RangedAttackHandler m_Ranged;

builder.Resolvers.AddSingleton<MeleeAttackHandler>().WithFactory(() => m_Melee);
builder.Resolvers.AddSingleton<RangedAttackHandler>().WithFactory(() => m_Ranged);

// Later:
foreach (var handler in Application<AttackHandler>.All()) {
    // Fan-out to every registered handler
}
```

(Or drop both into a single `ScriptableInjector` and call `m_Injector.Inject(builder)` for the same result.)

Or key them by ID with `DIBuilder<TKey>` for designer-assigned strategy selection:

```csharp
var builder = new DIBuilder<AbilityId>();
builder.AddSingleton<MeleeAttackHandler>(MeleeId).WithFactory(() => m_Melee);
builder.AddSingleton<RangedAttackHandler>(RangedId).WithFactory(() => m_Ranged);

var container = builder.Build();
var chosen = container.Get<AttackHandler>(currentAbilityId);
chosen.Execute(ctx);
```

## Where assets live

The rule is simple: **scripts live with the feature, `.asset` instances live in `Catalog/`.**

```
Features/Combat/
├── Configurations/
│   └── CombatConfiguration.cs       # script
├── Handlers/
│   └── MeleeAttackHandler.cs        # script
└── Loaders/
    ├── CombatLoader.cs              # script
    └── CombatLoader.asset           # exception: loader assets live here

Catalog/Combat/
├── CombatConfiguration.asset
├── Definitions/
│   └── SwordDefinition.asset
└── Handlers/
    └── MeleeAttackHandler.asset
```

The right-click scaffolding (`Buttr > Packages > Add to Package`) handles this layout for you. See [Editor Tooling](EditorTooling.md).
