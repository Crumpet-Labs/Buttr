using System;
using System.Collections.Generic;

namespace Buttr.Core {
    internal static class DIBuilderUtility {
        public static int CollectResolvedDependencies(this Dictionary<Type, IObjectResolver> registry, Type[] requirements, object[] output) {
            var count = 0;
            for (var i = 0; i < requirements.Length; i++) {
                var type = requirements[i];
                if (requirements[i] == null) continue;
                if (registry.TryGetValue(type, out var resolver)) {
                    output[count++] = resolver.Resolve();
                }
                else {
                    ButtrLog.Log($"Unable to locate Dependency in Container {requirements[i].FullName} ");
                }
            }
            return count;
        }

        public static int CollectUnresolvedTypes(this Dictionary<Type, IObjectResolver> registry, Type[] requirements, Type[] output) {
            var count = 0;
            for (var i = 0; i < requirements.Length; i++) {
                var type = requirements[i];
                if (registry.ContainsKey(type) == false) {
                    output[count++] = type;
                }
            }
            return count;
        }
    }
}