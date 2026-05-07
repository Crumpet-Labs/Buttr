# Editor Tooling

Buttr ships with two pieces of editor tooling: a **one-click project setup** and a **right-click scaffolding menu** for creating conventions-compliant packages and types. You rarely hand-write a new feature skeleton — the scaffolder does the tedious parts.

## Project setup

`Tools > Buttr > Setup Project`.

One click scaffolds the `_Project/` folder structure, creates the boot scene, generates `Program.cs` and `ProgramLoader` (script + asset), and adds the boot scene to build settings at index 0.

```
Assets/_Project/
├── {ProjectName}.asmdef
├── Main.unity
├── Program.cs
├── ProgramLoader.cs
├── Loaders/
│   └── ProgramLoader.asset
├── Core/
├── Features/
├── Shared/
└── Catalog/
```

Safe to re-run — it only creates files/folders that don't already exist. If you've already set up your project by hand, running the menu fills in any missing pieces without overwriting existing ones.

## Right-click scaffolding

In the Project window, right-click inside your `_Project/` folder → `Buttr > Packages`. Two groups: **create a new package** or **add to an existing package**.

### New packages

| Menu | What it scaffolds |
|---|---|
| `New Feature` | Full feature package — entry point, asmdef, Components (Model, Presenter, Mediator, View, Service), Contracts, Loader |
| `New Core` | Same as Feature, but created under `Core/` for engine-agnostic reusability |
| `New UI` | UI Toolkit variant — `{Name}Instance` MonoBehaviour, `{Name}View` as plain C#, scoped container lifecycle |

Each prompts for a name and gives you checkboxes for optional extras (Handlers, Behaviours, Identifiers, Configurations, Common, Exceptions). Classes are correctly named, sealed, and wired with constructor injection.

**UI packages** specifically scaffold:
- A `{Name}Instance : MonoBehaviour` that owns a `ScopeBuilder` and exposes a `UIDocument` field in the Inspector.
- A `{Name}View` as a plain C# class that takes the `UIDocument` via constructor injection — not a MonoBehaviour.

This keeps Views testable and decouples them from the GameObject lifecycle.

### Add to an existing package

`Add to Package` opens a type picker and drops a single correctly-templated file into the right subfolder of the package you right-clicked in. The types are grouped by their architectural layer:

- **Unity** — Controller, View
- **Data** — Model, Identifier, Definition, Configuration
- **Logic** — Presenter, System, Mediator, Handler, Behaviour
- **Infrastructure** — Service + Contract (scaffolded as a pair), Repository, Registry, Loader
- **Structure** — Extensions

(The `Instance` type is scaffolded only as part of `New UI` — it's tied to the UI Toolkit lifecycle pattern and doesn't make sense as a standalone add.)

The scaffolder infers the package name from the folder you right-click in and creates the correct subfolder (`Components/`, `Contracts/`, `MonoBehaviours/`, etc.) if it doesn't exist.

## What you don't get

Buttr's editor tooling doesn't:
- Modify or refactor existing files — only creates new ones.
- Enforce any conventions at runtime — analyzers handle that.
- Ship an opinionated Inspector for Buttr-created types. If you want custom Inspectors, they're yours to write.

## Under the hood

All scaffolding goes through `ButtrProjectScaffolder` in `Assets/Plugins/Buttr/Editor/Scaffolding/`. If you want to extend or customise the templates, that's where they live. Templates are plain `.cs` files with substitution markers — trivial to fork.

## Keyboard shortcuts

None bound by default. If you scaffold packages frequently, add a binding in `Edit > Shortcuts` to whichever scaffold command you use most.
