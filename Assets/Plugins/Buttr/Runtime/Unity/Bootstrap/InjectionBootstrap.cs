using Buttr.Injection;
using UnityEngine;

namespace Buttr.Unity {
    internal static class InjectionBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() {
            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;
        }

        private static void OnQuit() {
            Application.quitting -= OnQuit;
            InjectionProcessor.Clear();
        }
    }
}
