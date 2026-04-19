using System;
using System.IO;
using Buttr.Editor.Scaffolding;
using Buttr.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Buttr.Editor.SetupWizard {
    /// <summary>
    /// Executes the scaffolding steps for the setup wizard.
    /// Phase 1 (synchronous): folders, scripts, boot scene, build settings.
    /// Phase 2 (post-compilation via <see cref="ButtrPostCompileHook"/>): ProgramLoader asset + wiring.
    /// </summary>
    internal sealed class ButtrProjectScaffolder {
        internal const string SetupVersionKey = "Buttr.SetupVersion";
        internal const string PendingAssetCreationKey = "Buttr.PendingAssetCreation";
        internal const string CatalogFolder = "Catalog";
        internal const string RootFolder = "_Project";
        internal const string SceneName = "Main.unity";
        internal const string ProgramLoaderAssetName = "ProgramLoader.asset";

        private static readonly string[] ConventionFolders = {
            "Core",
            "Features",
            "Shared",
            CatalogFolder,
        };

        private readonly string m_ProjectName;
        private readonly string m_ButtrVersion;

        internal ButtrProjectScaffolder(string projectName, string buttrVersion) {
            m_ProjectName = ButtrPackageScaffolderUtility.SanitiseTypeName(projectName);
            m_ButtrVersion = buttrVersion;
        }

        // ── Static Queries ───────────────────────────────────────────

        internal static bool HasBeenSetUp => EditorPrefs.HasKey(SetupVersionKey);
        internal static string SetupVersion => EditorPrefs.GetString(SetupVersionKey, string.Empty);

        // ── Menu entry ───────────────────────────────────────────────

        [MenuItem("Tools/Buttr/Setup Project")]
        private static void SetupProjectMenuItem() {
            // Idempotent — each step checks for existing output and skips.
            // Running again after install re-asserts the convention folders / scripts / scene.
            const string buttrVersion = "2.2.0"; // TODO: read from package.json at runtime
            new ButtrProjectScaffolder(Application.productName, buttrVersion).ExecuteQuickSetup();
        }

        // ── Quick Setup ──────────────────────────────────────────────

        /// <summary>
        /// Runs Phase 1 of the Quick Setup scaffolding sequence.
        /// Creates all folders, scripts, boot scene, and build settings synchronously.
        /// Sets a flag for <see cref="ButtrPostCompileHook"/> to handle Phase 2
        /// (ProgramLoader asset creation and boot scene wiring) after Unity compiles the new scripts.
        /// </summary>
        internal void ExecuteQuickSetup() {
            Debug.Log("[Buttr] Starting Quick Setup...");

            try {
                CreateRootFolder();
                CreateSubFolders();
                GenerateAssemblyDefinition();
                GenerateProgramCs();
                GenerateProgramLoader();
                CreateBootScene();
                ConfigureBuildSettings();
                SetEditorPref();

                // Flag for post-compilation hook to create the asset and wire it
                EditorPrefs.SetInt(PendingAssetCreationKey, 1);

                Debug.Log("[Buttr] Phase 1 complete — folders, scripts, boot scene, and build settings configured.");
                Debug.Log("[Buttr] ProgramLoader asset will be created and wired automatically after script compilation.");
            }
            catch (Exception ex) {
                EditorPrefs.SetInt(PendingAssetCreationKey, 0);
                Debug.LogError($"[Buttr] Setup failed: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        // ── Phase 1 Steps ────────────────────────────────────────────

        private void CreateRootFolder() {
            var path = RootPath();

            if (Directory.Exists(path)) {
                Debug.Log($"[Buttr] {RootFolder}/ already exists — skipping");
                return;
            }

            Directory.CreateDirectory(path);
            Debug.Log($"[Buttr] Created {RootFolder}/");
        }

        private void CreateSubFolders() {
            var root = RootPath();
            var created = 0;

            foreach (var folder in ConventionFolders) {
                var path = Path.Combine(root, folder);

                if (Directory.Exists(path)) continue;

                Directory.CreateDirectory(path);
                created++;
            }

            if (created > 0)
                Debug.Log($"[Buttr] Scaffolded {created} convention folder{(created > 1 ? "s" : "")}: {string.Join(", ", ConventionFolders)}");
            else
                Debug.Log("[Buttr] All convention folders already exist");
        }

        private void GenerateAssemblyDefinition() {
            var fileName = $"{m_ProjectName}.asmdef";
            var path = Path.Combine(RootPath(), fileName);

            if (File.Exists(path)) {
                Debug.Log($"[Buttr] {fileName} already exists — skipping");
                return;
            }

            var sanitisedName = SanitiseNamespace(m_ProjectName);
            var content = $@"{{
    ""name"": ""{sanitisedName}"",
    ""rootNamespace"": ""{sanitisedName}"",
    ""references"": [
        ""Buttr.Core"",
        ""Buttr.Unity""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}}";

            File.WriteAllText(path, content);
            Debug.Log($"[Buttr] Generated {fileName} (namespace: {sanitisedName})");
        }

        private void GenerateProgramCs() {
            var path = Path.Combine(RootPath(), "Program.cs");

            if (File.Exists(path)) {
                Debug.Log("[Buttr] Program.cs already exists — skipping");
                return;
            }

            var sanitisedName = SanitiseNamespace(m_ProjectName);
            var content = $@"using System.Collections.Generic;
using Buttr.Core;

namespace {sanitisedName} {{
    public static class Program {{
        public static ApplicationContainer Main() => Main(CMDArgs.Read());

        private static ApplicationContainer Main(IDictionary<string, string> args) {{
            var builder = new ApplicationBuilder();

            // Register your packages here:
            // builder.UseMyFeature();

            return builder.Build();
        }}
    }}
}}
";

            File.WriteAllText(path, content);
            Debug.Log($"[Buttr] Generated Program.cs (namespace: {sanitisedName})");
        }

        private void GenerateProgramLoader() {
            var path = Path.Combine(RootPath(), "Core", "ProgramLoader.cs");

            if (File.Exists(path)) {
                Debug.Log("[Buttr] ProgramLoader.cs already exists — skipping");
                return;
            }

            var sanitisedName = SanitiseNamespace(m_ProjectName);
            var content = $@"using System.Threading;
using Buttr.Core;
using Buttr.Unity;
using UnityEngine;

namespace {sanitisedName} {{
    [CreateAssetMenu(fileName = ""ProgramLoader"", menuName = ""Buttr/Loaders/Program"", order = 0)]
    public sealed class ProgramLoader : UnityApplicationLoaderBase {{
        private ApplicationContainer m_Container;

        public override Awaitable LoadAsync(CancellationToken cancellationToken) {{
            m_Container = Program.Main();
            return AwaitableUtility.CompletedTask;
        }}

        public override Awaitable UnloadAsync() {{
            m_Container?.Dispose();
            return AwaitableUtility.CompletedTask;
        }}
    }}
}}
";

            File.WriteAllText(path, content);
            Debug.Log("[Buttr] Generated Loaders/ProgramLoader.cs");
        }

        private void CreateBootScene() {
            var scenePath = $"Assets/{RootFolder}/{SceneName}";
            var diskPath = Path.Combine(RootPath(), SceneName);

            if (File.Exists(diskPath)) {
                Debug.Log("[Buttr] Main.unity already exists — skipping");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootGo = new GameObject("Boot");
            bootGo.AddComponent<UnityApplicationBoot>();
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log("[Buttr] Created Main.unity with Boot GameObject and UnityApplicationBoot component");
        }

        private void ConfigureBuildSettings() {
            var scenePath = $"Assets/{RootFolder}/{SceneName}";
            var scenes = EditorBuildSettings.scenes;

            foreach (var existing in scenes) {
                if (string.Equals(existing.path, scenePath, StringComparison.OrdinalIgnoreCase)) {
                    Debug.Log("[Buttr] Main.unity already in build settings — skipping");
                    return;
                }
            }

            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            updated[0] = new EditorBuildSettingsScene(scenePath, true);
            Array.Copy(scenes, 0, updated, 1, scenes.Length);
            EditorBuildSettings.scenes = updated;

            Debug.Log("[Buttr] Main.unity added to build settings at index 0");
        }

        // ── Phase 2: Post-Compilation (called by ButtrPostCompileHook) ──

        /// <summary>
        /// Creates the ProgramLoader ScriptableObject asset and wires it into the boot scene.
        /// Called by <see cref="ButtrPostCompileHook"/> after Unity has compiled the generated scripts.
        /// </summary>
        internal static void ExecutePostCompileSetup() {
            Debug.Log("[Buttr] Running post-compilation setup...");

            try {
                CreateProgramLoaderAsset();

                Debug.Log("[Buttr] Setup complete — ProgramLoader asset created and wired to boot scene.");
            }
            catch (Exception ex) {
                Debug.LogError($"[Buttr] Post-compilation setup failed: {ex.Message}");
                Debug.LogException(ex);
            }
            finally {
                EditorPrefs.SetInt(PendingAssetCreationKey, 0);
            }
        }

        private static UnityApplicationLoaderBase CreateProgramLoaderAsset() {
            var assetPath = $"Assets/{RootFolder}/{CatalogFolder}/{ProgramLoaderAssetName}";

            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) != null) {
                Debug.Log($"[Buttr] {ProgramLoaderAssetName} already exists — skipping");
                return null;
            }

            // Find the compiled ProgramLoader type
            Type programLoaderType = null;

            foreach (var type in TypeCache.GetTypesDerivedFrom<UnityApplicationLoaderBase>()) {
                if (type.Name != "ProgramLoader") continue;

                programLoaderType = type;
                break;
            }

            if (programLoaderType == null) {
                Debug.LogWarning("[Buttr] Could not find compiled ProgramLoader type — create the asset manually via Create > Buttr > Loaders > Program");
                return null;
            }

            var instance = ScriptableObject.CreateInstance(programLoaderType);
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[Buttr] Created {CatalogFolder}/{ProgramLoaderAssetName}");
            return instance as UnityApplicationLoaderBase;
        }

        // ── EditorPref ───────────────────────────────────────────────

        private void SetEditorPref() {
            var version = string.IsNullOrEmpty(m_ButtrVersion) ? "unknown" : m_ButtrVersion;
            EditorPrefs.SetString(SetupVersionKey, version);
        }

        // ── Utilities ────────────────────────────────────────────────

        private static string RootPath() => Path.Combine(Application.dataPath, RootFolder);

        /// <summary>
        /// Sanitises a project name into a valid C# namespace identifier.
        /// Strips invalid characters, ensures it starts with a letter or underscore.
        /// </summary>
        internal static string SanitiseNamespace(string name) {
            if (string.IsNullOrWhiteSpace(name)) return "Project";

            var chars = name.ToCharArray();
            var result = new char[chars.Length];
            var index = 0;

            for (var i = 0; i < chars.Length; i++) {
                var c = chars[i];

                if (index == 0) {
                    if (char.IsLetter(c) || c == '_')
                        result[index++] = c;

                    continue;
                }

                if (char.IsLetterOrDigit(c) || c == '_')
                    result[index++] = c;
            }

            if (index == 0) return "Project";

            return new string(result, 0, index);
        }
    }

    /// <summary>
    /// Runs after every domain reload. If a pending asset creation flag is set,
    /// creates the ProgramLoader ScriptableObject asset and wires it into the boot scene.
    /// </summary>
    [InitializeOnLoad]
    internal static class ButtrPostCompileHook {
        static ButtrPostCompileHook() {
            switch (EditorPrefs.GetInt(ButtrProjectScaffolder.PendingAssetCreationKey, 0)) {
                case 1: ButtrProjectScaffolder.ExecutePostCompileSetup(); break;
                default: return;
            }

            EditorApplication.delayCall += ButtrProjectScaffolder.ExecutePostCompileSetup;
        }
    }
}