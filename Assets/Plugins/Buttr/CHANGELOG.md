# Changelog

All notable changes to Buttr will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.4.0] - 2026-04-21

Switches from vendoring the Buttr.Core DLLs to depending on the new `com.crumpetlabs.buttr` UPM package. No runtime or API changes.

### Changed

- **Buttr.Core and Buttr.Injection are no longer vendored inside `Runtime/Lib/`.** They now arrive via a standard UPM dependency on `com.crumpetlabs.buttr` (1.3.3), supplied by [Buttr.Core](https://github.com/Crumpet-Labs/Buttr.Core). Consumers will see the Core DLLs land under `Packages/com.crumpetlabs.buttr/Runtime/Lib/` instead of inside this package. Asmdef references (`Buttr.Core`, `Buttr.Injection`) resolve identically because assembly names are unchanged.
- **`Buttr.Core.Analyzers.dll` stays vendored** in `Analyser/` alongside the Unity source generator. Roslyn analyzer packaging is Unity-specific and doesn't ship in the Core UPM package.

### Migration

Automatic for Unity projects that use a manifest pointing at `com.crumpetlabs.buttr.unity` via git URL — UPM resolves the new `com.crumpetlabs.buttr` dep transitively. Projects that had both Buttr.Core DLLs AND another package vendoring the same DLLs (duplicate-assembly error) will now resolve cleanly because only one package ships them.

If you were referencing the vendored DLL paths directly (e.g. in a custom csproj), update to `Packages/com.crumpetlabs.buttr/Runtime/Lib/Buttr.Core.dll`.

## [2.3.1] - 2026-04-20

Tracks [Buttr.Core 1.3.2](https://github.com/Crumpet-Labs/Buttr.Core/releases/tag/v1.3.2). Renames the bulk-resolution surface on the global `Application` container.

### Changed

- **`Application.All<T>()` → `Application<T>.All()`.** The bulk-resolution surface now lives on the same generic type as its single-instance counterpart (`Application<T>.Get()`). The non-generic `Application` class is removed. Behaviour is identical — same struct enumerator, zero-alloc iteration, hidden registrations still excluded. Side benefit: no more ambiguity against `UnityEngine.Application` at call sites that also `using UnityEngine;`.
- **Vendored DLLs refreshed** to Buttr.Core 1.3.2, Buttr.Injection 1.3.2, and Buttr.Core.Analyzers 1.3.2. `.meta` GUIDs preserved.

### Migration

Mechanical. Replace `Application.All<TFoo>()` with `Application<TFoo>.All()` at each call site. If your call site also has `using UnityEngine;`, you can drop any `global::` qualifications or `using` aliases that were working around the previous `Application` name clash.

## [2.3.0] - 2026-04-20

Tracks [Buttr.Core 1.3.1](https://github.com/Crumpet-Labs/Buttr.Core/releases/tag/v1.3.1). Drop-in upgrade — no Unity-facing API changes.

### Added

- **Aliasing** — `.As<TAlias>()` on any registration lets consumers resolve the same underlying instance through multiple interfaces. Surfaces on `DIBuilder`, `ScopeBuilder`, and the static `Application` container.
- **Bulk resolution** — `container.All<T>()` (and `Application.All<T>()`) returns every registration whose concrete type is assignable to `T`. Iteration is zero-alloc via a struct enumerator. Hidden registrations are excluded.
- **Two new compile-time diagnostics** from the bundled `Buttr.Core.Analyzers`:
  - `BUTTR013` — alias is not a supertype of the concrete registration (error, with code fix).
  - `BUTTR014` — duplicate alias key across registrations (error, with code fix).

### Changed

- **Analyzer ownership split** — `BUTTR004`, `BUTTR006`, `BUTTR012` now fire from `Buttr.Core.Analyzers.dll` (shipped under `Assets/Plugins/Buttr/Analyser/`) instead of `Buttr.Unity.SourceGeneration.dll`. Rule IDs and messages are unchanged.
- **`BUTTR006` scoped per-builder-instance.** Each `ApplicationBuilder` / `DIBuilder` / `ScopeBuilder` is evaluated independently — independent instances in separate files no longer cross-contaminate. Receivers that can't be symbolically identified (inline `new DIBuilder()`, chained fluent returns) fall back to per-source-file grouping. Severity stays a Warning — last-wins override is a legitimate pattern (test doubles, late-binding overrides); it's still surfaced, it just doesn't break the build.
- **Vendored DLLs refreshed** to Buttr.Core 1.3.1 and Buttr.Injection 1.3.1. `Runtime/Lib/Buttr.Core.dll` and `Runtime/Lib/Buttr.Injection.dll` updated; `.meta` GUIDs preserved.

### Migration

Most consumers need no code changes. `.As<>()` and `All<T>()` are additive, and per-builder `BUTTR006` scoping means typical test suites with `[SetUp]`-fresh builders don't trip it.

- **Deliberate within-builder duplicates** (e.g., a test verifying last-wins overwrite on one builder) still emit a `BUTTR006` warning. The warning doesn't fail the build, but if you've enabled *warnings-as-errors* (`-warnaserror`), suppress per-file with `#pragma warning disable BUTTR006` and a short comment explaining the intent.
- **Custom implementers of `IConfigurable<TConcrete>` or `IConfigurableCollection`** (rare — these interfaces were designed for consuming, not implementing) must add the new members: `IConfigurable<TConcrete> As<TAlias>()` and `IConfigurableCollection As<TConcrete, TAlias>()` respectively.
- **Analyzer-assembly-name suppressions** for `BUTTR004`/`006`/`012` (uncommon) should reference `Buttr.Core.Analyzers` instead of `Buttr.Unity.SourceGeneration`. Suppressing by rule ID (the normal path) keeps working.

## [2.2.0] - 2026-04-19

### Package split

The engine-agnostic core has been extracted into a separate .NET library at
[Crumpet-Labs/Buttr.Core](https://github.com/Crumpet-Labs/Buttr.Core) (v1.0.0).
This Unity package now vendors `Buttr.Core.dll` and `Buttr.Injection.dll` from
that repository under `Runtime/Lib/` and adds the Unity-specific bridge on top.

### Changed

- **Package name**: `com.crumpetlabs.buttr` → `com.crumpetlabs.buttr.unity`. Existing users must update their `manifest.json` entry — the git URL stays the same.
- **Display name**: "Buttr" → "Buttr for Unity".
- **No more `Runtime/Core/` or `Runtime/Injection/` source folders.** Their assemblies (`Buttr.Core` and `Buttr.Injection`) now ship as precompiled DLLs in `Runtime/Lib/`. Public API surface is unchanged: `ApplicationBuilder`, `Application<T>`, `DIBuilder`, `ScopeBuilder`, `[Inject]`, `IInjectable`, `InjectionProcessor.Register`/`Inject` all behave as before.
- **Source generator**: renamed from `SourceGeneration.dll` to `Buttr.Unity.SourceGeneration.dll` (same asset GUID, same labels, same generated output).
- **CMDArgs**: no longer auto-initialises via `[RuntimeInitializeOnLoadMethod]`. The Unity package now registers a bootstrap (`Buttr.Unity.CMDArgsBootstrap`) that calls `CMDArgs.Initialize(Environment.GetCommandLineArgs())` at `SubsystemRegistration`. End users see no behavioural change.
- **Internal logging**: now routed through a new `Buttr.Core.ButtrLog` facade. The Unity package registers `Buttr.Unity.UnityButtrLogger` at `SubsystemRegistration` to route into `UnityEngine.Debug.Log*`.
- **`InjectionProcessor` split**: scene-walking helpers (`InjectScene`, `InjectActiveScene`, `InjectAllLoadedScenes`, `InjectGameObject`, `InjectSelfAndChildren`) have moved to a new static class `Buttr.Unity.Injection.InjectionProcessorUnityExtensions`. The pure `Buttr.Injection.InjectionProcessor` now only exposes `Register<T>`, `Inject(object)`, and `Clear()`.
- **Unity-specific injection types moved**: `MonoInjector`, `SceneInjector`, `MonoInjectStrategy`, and `BehaviourInjectorTooltips` now live under `Runtime/Unity/Injection/` with namespace `Buttr.Unity.Injection` (previously `Buttr.Injection`). Prefab references are preserved via their existing script GUIDs.
- **`AwaitableUtility`**: moved from `Runtime/Core/Utility/` to `Runtime/Unity/Utility/`. Namespace remains `Buttr.Core` for source compatibility — existing code using `using Buttr.Core; AwaitableUtility.CompletedTask;` continues to compile without changes.
- **Setup Wizard replaced by a menu item**: the `EditorWindow`-based setup wizard has been retired. Quick Setup now runs via `Tools > Buttr > Setup Project` — one click, same scaffolding behaviour (convention folders, `Program.cs`, `ProgramLoader.cs`, boot scene, build settings, `ProgramLoader` asset wiring). The wizard no longer auto-opens on first project load; existing `EditorPrefs` setup-state keys are unchanged.

### Removed

- Dead interface `Buttr.Core.IApplicationRunner` — internal, zero references across the codebase.
- `Assets/Plugins/Buttr/Runtime/Core/Buttr.Core.asmdef` and `Assets/Plugins/Buttr/Runtime/Injection/Buttr.Injection.asmdef` — assemblies now ship as precompiled DLLs.
- `Editor/SetupWizard/`: wizard UI, UXML, USS, images, models, views, mediators, presenter, enums, and wizard-only utility helpers. Only `ButtrProjectScaffolder` (and `ButtrPostCompileHook`) remain — they run the same setup work triggered now by the menu item.
- The "Skip Conventions" setup mode — it existed only as a wizard option and performed no scaffolding. Users who want Buttr without conventions simply don't run the menu item.

### Migration

If you only use `[Inject]`, `IInjectable`, `ApplicationBuilder`, `Application<T>`, `ScopeBuilder`, `DIBuilder`, or the `SceneInjector` / `MonoInjector` MonoBehaviours, no code changes are required.

If your own asmdef file referenced `Buttr.Core` or `Buttr.Injection` by name or GUID, change it to reference `Buttr.Unity` (which transitively exposes both). If you need direct API access without going through `Buttr.Unity`, add `"Buttr.Core.dll"` and `"Buttr.Injection.dll"` to its `precompiledReferences` (with `"overrideReferences": true`).

If you called any of the Unity-only `InjectionProcessor` scene helpers (`InjectScene`, `InjectActiveScene`, `InjectAllLoadedScenes`, `InjectGameObject`, `InjectSelfAndChildren`) directly, change the class name to `InjectionProcessorUnityExtensions` (in namespace `Buttr.Unity.Injection`). Method signatures are unchanged.

In your project's `Packages/manifest.json`, update the package name from `com.crumpetlabs.buttr` to `com.crumpetlabs.buttr.unity`. The git URL is unchanged.

## [2.1.1] - 2026-03-14

### Fixed

- **Warnings relating to uxml on install** removed unecessary uxml from the project and updated the asset paths relative
- **Project name scaffolding** some projects have weird names. So now we protect against that inside the scaffolding logic

## [2.1.0] - 2026-03-14

### Added

#### UI Package Scaffolding
- **Right-click "New UI Package":** Scaffolds a UI Toolkit feature package with Package extension, asmdef, README, Model, Presenter, Mediator, View, Service + Contract, and Instance. Views are plain C# classes that receive a `UIDocument` via constructor injection. The Instance MonoBehaviour owns the scoped container lifecycle on the GameObject.
- **`{Name}Instance` convention:** A MonoBehaviour that builds a `ScopeBuilder`, injects ScriptableObjects via `ScriptableInjector`, registers the feature's package with a factory override for the View's `UIDocument` dependency, and disposes the container on `OnDestroy`. Lives in `MonoBehaviours/`. This naming is subject to change in a future version.
- **`{Name}View` as plain C# class:** Views are no longer MonoBehaviours. They are plain C# classes that take a `UIDocument` reference via constructor injection and query visual elements from it. Views live in `Components/` alongside other injected types.

### Changed

- **View convention updated:** Views are now plain C# classes, not MonoBehaviours. They observe and display Model data via constructor-injected dependencies. For UI Toolkit features, the View receives a `UIDocument` and queries elements from it. This moves Views from the Unity layer to the Logic layer in the Quick Reference table.
- **View folder location:** Views now live in `Components/` (as injected types) rather than `MonoBehaviours/`. The `MonoBehaviours/` folder is reserved for Controllers and Instances.
- **`ApplicationLifetime` renamed to `ApplicationContainer`:** The return type of `ApplicationBuilder.Build()` is now `ApplicationContainer`. All generated code (Program.cs, ProgramLoader, Core Loaders) reflects this change.
- **`Database/` references removed from GitHub README:** The folder was renamed to `Catalog/` in 2.0.0 but the GitHub README still referenced `Database/` in the scaffolded structure and project structure table. Now corrected.
- **Mediator template updated:** The scaffolded Mediator now takes a Presenter, Model, and View via constructor injection — reflecting the standard observation pattern. There is no enforced rule on what gets injected into a Mediator; the template provides a starting point.
- **Scoped container examples use `builder.Resolvers`:** README examples for `ScopeBuilder` now use `builder.Resolvers.AddSingleton` / `AddTransient` (the preferred API). `builder.AddSingleton` remains available as a convenience shorthand.
- **Project README and GitHub README updated:** Both READMEs reflect the View convention change, Instance pattern, `ApplicationContainer` rename, UI package scaffolding, and corrected folder references.

### Fixed

- **Setup wizard scaffolding failures:** Fixed issues where the setup wizard was not completing correctly.
- **Template generation errors:** Fixed incorrect output in several scaffolding templates.
- **GitHub README folder inconsistency:** The scaffolded structure and project structure table referenced `Database/` instead of `Catalog/`, which was renamed in 2.0.0.
- **GitHub README missing scaffolded files:** The scaffolded structure now includes `{ProjectName}.asmdef` and `Program.cs`, which were generated by the wizard but not shown in the README.
- **`LoadAsync` signature inconsistency:** Code samples used `async Awaitable LoadAsync` where the method body was synchronous. Updated to match the generated template pattern (no `async` keyword, returns `AwaitableUtility.CompletedTask`).

## [2.0.0] - 2026-03-09

### ⚠️ Breaking Changes

- **Folder structure renamed:** `_Buttr/` is now `_Project/`. Existing projects must rename their root folder.
- **`Database/` renamed to `Catalog/`:** ScriptableObject data assets now live in `Catalog/`, organised by feature.
- **`[InjectScope("key")]` removed:** Unified into `[Inject("scope")]` with an optional scope parameter. All usages of `[InjectScope]` must be updated to `[Inject("scope")]`.
- **Disk-based source generation removed:** The `EditorApplication.delayCall` injection code generator, `InjectionCacheManager`, `InjectionCodeGenerator`, `InjectionCodeGeneratorUtility`, `InjectionConfiguration`, and all cache types (`CachedEntry`, `CachePair`, `CacheWrapper`) have been removed. Source generation is now handled entirely by Roslyn source generators at compile time.
- **Generated `*_Inject.g.cs` files removed from disk:** Injection code is now generated in-memory by the compiler. Delete any previously generated files from your project.
- **Controller renamed from Presenter (MonoBehaviour):** The MonoBehaviour coordination type is now called `Controller`. The `Presenter` suffix is reserved for plain C# classes that mutate Model state. This resolves the previous naming collision.
- **Loader assets relocated:** Loader `.asset` files now live in the package's `Loaders/` folder alongside the Loader script, not in `Catalog/`.

### Added

#### Source Generation & Analyzers
- **Roslyn source generators** replacing the entire editor injection pipeline. Zero runtime reflection, no files on disk, no stale cache, incremental compilation support.
- **BUTTR001** — Error: `[Inject]` used on a non-partial MonoBehaviour.
- **BUTTR002** — Warning: Hidden type injected via `[Inject]` attribute.
- **BUTTR003** — Warning: Hidden type resolved via `Application.Get<T>()`.
- **BUTTR004** — Warning: Unresolvable constructor parameter (type not registered in any visible container).
- **BUTTR005** — Warning: Registered type never used (no `[Inject]` field, no `Get<T>()` call, no constructor parameter referencing it).
- **BUTTR006** — Error: Duplicate registration of the same type in the same container.
- **BUTTR008** — Warning: Registration issues in configurable collections.
- **BUTTR009** — Warning: Magic string used as scope key instead of a constant.

#### Setup Wizard
- **Two-path setup wizard:** Quick Setup (one-click scaffolding with all conventions) and Skip Conventions (standalone DI framework, no scaffolding).
- **Project scaffolding:** Generates `_Project/` folder structure, `Program.cs`, `ProgramLoader.cs`, assembly definition, boot scene with `UnityApplicationBoot`, and build settings configuration.
- **Post-compile asset creation:** `ButtrPostCompileHook` automatically creates `ProgramLoader.asset` after Unity compiles the generated scripts.
- **Setup version tracking:** `EditorPrefs` stores the Buttr version used during setup, enabling future upgrade detection.

#### Editor Tooling
- **Right-click "New Feature":** Scaffolds a full feature package with Package extension, asmdef, README, Model, Presenter, Mediator, Service + Contract, View, and Loader. Optional additions: Handlers, Behaviours, Identifiers, Configurations, Common, Exceptions.
- **Right-click "New Core Package":** Same as New Feature but generates `ApplicationBuilder` registrations instead of `ScopeBuilder`.
- **Right-click "Add to Package":** 17 individual type scaffolders grouped by architectural layer (Unity, Data, Logic, Infrastructure, Structure). Each creates the correct subfolder and templated file with proper naming, namespace, and constructor injection.
- **Layered menu structure:** Add to Package options grouped as Unity (Controller, View), Data (Model, Identifier, Definition, Configuration), Logic (Presenter, System, Mediator, Handler, Behaviour), Infrastructure (Service + Contract, Repository, Registry, Loader), Structure (Extensions).
- **Smart package detection:** Menu items use asmdef-based package root detection. Add to Package options only appear when right-clicking inside a package.
- **Convention structure validation:** All scaffolding menu items are greyed out unless `_Project/` with an asmdef exists.
- **Per-package assembly definitions:** Each scaffolded package gets its own asmdef with correct namespace and Buttr references.
- **Dependency-aware scaffolding:** Adding a Registry automatically scaffolds its Identifier and Controller dependencies. Adding a System or Behaviour scaffolds the Context struct. Adding a Service scaffolds the Contract interface.
- **Post-compile ScriptableObject creation:** Loader and Configuration assets are queued and created automatically after Unity compiles the generated scripts.
- **Package README generation:** Each scaffolded package includes a README with package type, usage example, and placeholder sections.

#### Runtime
- **ScriptableInjector:** Expression tree-based utility for injecting ScriptableObjects into any Buttr container without per-type Loader boilerplate. Drag ScriptableObjects into the inspector list and they're registered automatically.
- **`CMDArgs` utility:** Static utility that parses command line arguments via `RuntimeInitializeOnLoadMethod`, making launch arguments available to `Program.Main()`.

#### Architecture Conventions
- **17 named type conventions** across 5 layers (Unity, Data, Logic, Infrastructure, Structure) with defined suffixes, responsibilities, and folder locations.
- **System/Behaviour `Tick` pattern:** Systems use `Tick(Context ctx)` instead of `Update` to communicate loop-agnostic execution. Behaviours implement the same `Tick` method.
- **Context structs:** Systems and Behaviours share a per-feature Context struct that bundles tick parameters.
- **Registry with disposable registration:** `Register()` returns `IDisposable` for automatic deregistration. Private `Deregister` method prevents manual deregistration.
- **Repository as interface:** Repository scaffolds as a generic `IRepository<TKey, TData>` contract in `Contracts/`, not a concrete class.
- **Scope boundary documentation:** Design Philosophy section clarifies that Buttr handles feature-level singleton architecture, not per-instance entity management.

### Changed

- **`Program.cs` convention:** Application composition entry point following .NET conventions. Generated by the setup wizard with `CMDArgs` overload pattern.
- **`ProgramLoader` convention:** Thin `UnityApplicationLoaderBase` shell that calls `Program.Main()`. Generated alongside `Program.cs`.
- **Extensions are internal:** Extension classes are generated as `internal static class` to keep package internals private.
- **Feature packages use `ScopeBuilder`:** Scaffolded features register dependencies in scoped containers with a const scope key. Core packages use `ApplicationBuilder`.
- **ScriptableObject `CreateAssetMenu` uses project namespace:** Generated Loaders, Definitions, and Configurations use `{ProjectName}/Loaders/{Name}` instead of `Buttr/Loaders/{Name}`.
- **Catalog no longer stores Loader assets:** Loader assets live in the package's `Loaders/` folder. Catalog stores only Configurations, Definitions, and Handlers.
- **Project README updated:** Comprehensive architecture guide with all 17 conventions, feature structure examples, Catalog mirroring, and editor tooling documentation.
- **GitHub README updated:** Reflects two-path wizard, editor tooling features, updated scaffold structure, and corrected roadmap.

### Removed

- **`InjectionConfiguration` ScriptableObject:** No longer needed — source generation is handled by Roslyn at compile time.
- **All disk-based injection cache types:** `CachedEntry`, `CachePair`, `CacheWrapper`, `InjectionCacheManager`.
- **`InjectionCodeGenerator` and related editor classes:** Replaced entirely by Roslyn source generators.
- **`buttr.config` file:** Configuration file concept removed. Setup state tracked via EditorPrefs.

### Fixed

- **Stale generated file compilation errors:** Moving from disk-based to in-memory source generation eliminates the class of bugs where removing an `[Inject]` field left orphaned generated code that caused compilation errors.
- **Cross-assembly analyzer false positives:** Analyzers use warning-level diagnostics for cross-container issues while keeping definitively wrong problems (like missing `partial`) as errors.