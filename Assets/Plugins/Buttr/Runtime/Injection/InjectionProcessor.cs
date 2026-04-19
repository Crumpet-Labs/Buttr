using System;
using System.Collections.Generic;
using Buttr.Core;

namespace Buttr.Injection {
    /// <summary>
    /// Engine-agnostic registry of per-type injection delegates.
    /// Engine-specific scene / hierarchy walking lives in an adapter layer.
    /// </summary>
    public static class InjectionProcessor {
        private static readonly Dictionary<Type, Action<object>> m_Injectors = new();

        public static void Register<T>(Action<T> injector) where T : IInjectable {
            if (injector == null)
                throw new InjectionException("Cannot Register a null injector");

            m_Injectors.Add(typeof(T), obj => injector((T)obj));
        }

        public static void Inject(object instance) {
            if (instance == null) throw new InjectionException("Cannot inject into a null instance");
            if (instance is not IInjectable injectable)
                throw new InjectionException("Can only inject into IInjectable instances");

            if (injectable.Injected) {
                ButtrLog.LogWarning("Attempting to Inject into the same object twice... Skipping object");
                return;
            }

            if (m_Injectors.TryGetValue(instance.GetType(), out var del)) {
                del(instance);
                injectable.Injected = true;
            }
            else {
                throw new InjectionException($"[Injection] No injector registered for type {instance.GetType()}");
            }
        }

        public static void Clear() {
            m_Injectors.Clear();
        }
    }
}
