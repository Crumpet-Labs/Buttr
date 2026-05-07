using System;
using System.IO;
using Buttr.Editor.SetupWizard;
using UnityEditor;
using UnityEngine;

namespace Buttr.Editor.Scaffolding {
    [InitializeOnLoad]
    internal static class ButtrPostCompileHook {
        private const string k_PENDING_ASSETS = "Buttr.PendingAssets";
        
        static ButtrPostCompileHook() {
            var pending = EditorPrefs.GetString(k_PENDING_ASSETS, string.Empty);

            if (string.IsNullOrEmpty(pending) && false == EditorPrefs.GetBool(ButtrProjectScaffolder.PendingAssetCreationKey, false))
                return;

            EditorApplication.delayCall += RunPostCompileSetup;
        }

        private static void RunPostCompileSetup() {
            if (EditorPrefs.GetBool(ButtrProjectScaffolder.PendingAssetCreationKey, false))
                ButtrProjectScaffolder.ExecutePostCompileSetup();

            var pending = EditorPrefs.GetString(k_PENDING_ASSETS, string.Empty);

            if (string.IsNullOrEmpty(pending)) return;

            EditorPrefs.DeleteKey(k_PENDING_ASSETS);

            var entries = pending.Split(';');

            foreach (var entry in entries) {
                var parts = entry.Split('|');
                if (parts.Length != 2) continue;

                CreatePendingAsset(parts[0], parts[1]);
            }
        }

        private static void CreatePendingAsset(string typeName, string assetPath) {
            assetPath = assetPath.Replace('\\', '/');
    
            if (assetPath.Contains(Application.dataPath.Replace('\\', '/'))) {
                assetPath = "Assets" + assetPath.Substring(Application.dataPath.Replace('\\', '/').Length);
            }
    
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) != null) {
                Debug.Log($"[Buttr] {Path.GetFileName(assetPath)} already exists — skipping");
                return;
            }

            Type targetType = null;

            foreach (var type in TypeCache.GetTypesDerivedFrom<ScriptableObject>()) {
                if (type.Name != typeName) continue;
                targetType = type;
                break;
            }

            if (targetType == null) {
                Debug.LogWarning($"[Buttr] Could not find type '{typeName}' — create the asset manually");
                return;
            }

            // Using absolute path for filesystem operations
            var absoluteDir = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            absoluteDir = Path.GetDirectoryName(absoluteDir)?.Replace('\\', '/');
    
            if (false == string.IsNullOrEmpty(absoluteDir) && false == Directory.Exists(absoluteDir))
                Directory.CreateDirectory(absoluteDir);

            var instance = ScriptableObject.CreateInstance(targetType);
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Buttr] Created {Path.GetFileName(assetPath)}");
        }
    }
}