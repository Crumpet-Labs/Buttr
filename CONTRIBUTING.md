# Contributing

Buttr.Unity is the Unity bridge for the engine-agnostic [Buttr.Core](https://github.com/Crumpet-Labs/Buttr.Core) DI library. Both repositories share the same contribution standards — see the canonical guide:

**→ [Buttr.Core/Docs/Contributing.md](https://github.com/Crumpet-Labs/Buttr.Core/blob/main/Docs/Contributing.md)**

It covers:

- Discussing architectural changes before opening a PR
- Code style, naming, and conventions
- Test discipline (`dotnet test` must stay green)
- Commit message format
- Adding new analyzer rules

## Where work belongs

- **Engine-agnostic** changes (DI container, builders, resolvers, lifetimes, scopes, analyzer rules that aren't Unity-specific) → [Buttr.Core](https://github.com/Crumpet-Labs/Buttr.Core).
- **Unity-specific** changes (MonoBehaviour injection, `SceneInjector`/`MonoInjector`, `ScriptableInjector`, `UnityApplicationBoot`/`Loader`, the `[Inject]` source generator, editor scaffolding) → this repository.

## Reporting issues

Use [GitHub Issues](https://github.com/Crumpet-Labs/Buttr.Unity/issues). Include the Buttr.Unity version, Buttr.Core version, Unity version, and a minimal repro.

For Code of Conduct issues, see [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).
