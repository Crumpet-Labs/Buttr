using System;
using System.Collections.Generic;

namespace Buttr.Core {
    internal static class ServiceResolverUtilities {
        public static void TryResolve<TConcrete>(this Dictionary<Type, IObjectResolver> registry, Type[] requirements, object[] output) {
            if (requirements.Length == 0) return;
            
            var internalResolved = ArrayPool<object>.Get(requirements.Length);
            var applicationResolved = ArrayPool<object>.Get(requirements.Length);
            var internalMissing = ArrayPool<Type>.Get(requirements.Length);
            var combined = ArrayPool<object>.Get(requirements.Length);

            try {
                var internalCount = registry.CollectResolvedDependencies(requirements, internalResolved);
                if (internalCount == requirements.Length) {
                    // All dependencies resolved internally — skip ApplicationRegistry
                    Array.Copy(internalResolved, output, requirements.Length);
                }
                else {
                    registry.CollectUnresolvedTypes(requirements, internalMissing);
                    var externalCount = ApplicationRegistry.GetDependencies(internalMissing, applicationResolved);

                    var totalCount = internalCount + externalCount;

                    if (totalCount != requirements.Length)
                        throw new ObjectResolverException($"Unable to resolve all dependencies for {typeof(TConcrete)}");

                    Array.Copy(internalResolved, 0, combined, 0, internalCount);
                    Array.Copy(applicationResolved, 0, combined, internalCount, externalCount);

                    // Reorder to match constructor
                    for (var i = 0; i < requirements.Length; i++) {
                        var required = requirements[i];
                        for (var j = 0; j < totalCount; j++) {
                            if (combined[j]?.GetType() == required) {
                                output[i] = combined[j];
                                break;
                            }
                        }

                        if (output[i] == null)
                            throw new ObjectResolverException($"Missing dependency of type {required} for {typeof(TConcrete)}");
                    }
                }
            }
            finally {
                ArrayPool<object>.Release(internalResolved);
                ArrayPool<object>.Release(applicationResolved);
                ArrayPool<Type>.Release(internalMissing);
                ArrayPool<object>.Release(combined);
            }
        }
        
        public static bool TryValidate(this object[] foundDependencies, Type[] requirements) {
            if (foundDependencies.Length != requirements.Length)
                return false;

            for (var i = 0; i < foundDependencies.Length; i++) {
                var instance = foundDependencies[i];
                if (instance == null) return false;

                var instanceType = instance.GetType();
                var requiredType = requirements[i];

                if (requiredType.IsAssignableFrom(instanceType) == false) {
                    ButtrLog.LogWarning($"Dependency of type {instanceType} does not satisfy required type {requiredType}.");
                    return false;
                }
            }

            return true;
        }
    }
}