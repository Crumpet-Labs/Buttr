using Buttr.Core;
using UnityEngine;

namespace Buttr.Unity {
    internal static class ButtrLogBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() {
            ButtrLog.SetLogger(new UnityButtrLogger());
        }
    }
}
