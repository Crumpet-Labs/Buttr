using System;
using System.Collections.Generic;

namespace Buttr.Core {
    internal static class ApplicationRegistry {
        private static readonly Dictionary<Type, IObjectResolver> s_Resolvers;

        static ApplicationRegistry() => s_Resolvers = new();
        
        public static int GetDependencies(Type[] requirements, object[] output) {
            return s_Resolvers.CollectResolvedDependencies(requirements, output);
        }

        internal static void Register<T>(IObjectResolver resolver) {
            if (s_Resolvers.TryAdd(typeof(T), resolver) == false) {
                throw new ObjectResolverException($"Failed to add resolver to Application Registry, Has {typeof(T).Name} already been added?");
            }
        }

        internal static void Remove<T>() {
            s_Resolvers.Remove(typeof(T));
        }
    }
}