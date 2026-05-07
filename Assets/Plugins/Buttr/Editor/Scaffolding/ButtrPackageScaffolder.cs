using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Buttr.Editor.Scaffolding {
    /// <summary>
    /// Scaffolds full packages following Buttr conventions.
    /// All methods are synchronous and write directly to disk.
    /// </summary>
    internal static class ButtrPackageScaffolder {
        /// <summary>
        /// Scaffolds a full package with core structure and optional additions.
        /// Called by the popup window for "New Feature" and "New Core Package".
        /// </summary>
        internal static void CreatePackage(string parentFolder, string packageName, PackageType type, PackageOptions options) {
            var root = Path.Combine(parentFolder, packageName);

            if (Directory.Exists(root)) {
                Debug.LogWarning($"[Buttr] Package '{packageName}' already exists at {root}");
                return;
            }

            var ns = parentFolder.ResolveNamespace(packageName);
            var projectName = ButtrPackageScaffolderUtility.GetRootNamespace();

            Debug.Log($"[Buttr] Creating {type} package: {packageName}...");

            Directory.CreateDirectory(root);

            root.WriteFile($"{packageName}Package.cs",
                new ButtrPackageExtensionTemplate(ns, projectName, type).Generate());

            root.WriteFile($"{ns}.asmdef",
                new ButtrAsmdefTemplate(ns).Generate());

            root.WriteFile("README.md",
                new ButtrREADMETemplate(packageName, type).Generate());

            new AddModelCommand(root, false).Execute();
            new AddPresenterCommand(root, false).Execute();
            new AddMediatorCommand(root, false).Execute();
            new AddServiceAndContractCommand(root, false).Execute();

            switch (type) {
                case PackageType.Feature:
                case PackageType.Core:
                    new AddLoaderCommand(root, type, false).Execute();
                    break;
                case PackageType.UI:
                    new AddViewCommand(root, false).Execute();
                    new AddInstanceCommand(root, type, false).Execute();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if(options.HasFlag(PackageOptions.Definitions))
                new AddDefinitionCommand(root, false).Execute();
            
            if (options.HasFlag(PackageOptions.Handlers))
                new AddHandlerCommand(root, false).Execute();

            if (options.HasFlag(PackageOptions.Behaviours))
                new AddBehaviourCommand(root, false).Execute();

            if (options.HasFlag(PackageOptions.Identifiers))
                new AddIdentifierCommand(root, false).Execute();
            
            if(options.HasFlag(PackageOptions.Controller))
                new AddControllerCommand(root, false).Execute();

            if (options.HasFlag(PackageOptions.Configurations))
                new AddConfigurationCommand(root, false).Execute();

            if (options.HasFlag(PackageOptions.Common) && false == options.HasFlag(PackageOptions.Behaviours))
                root.CreateSubFolder("Common");

            if (options.HasFlag(PackageOptions.Exceptions))
                new AddExceptionCommand(root, false).Execute();

            AssetDatabase.Refresh();
            Debug.Log($"[Buttr] {type} package '{packageName}' created successfully.");
        }
    }
}