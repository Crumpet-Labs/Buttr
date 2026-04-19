using System;
using Buttr.Core;
using UnityEngine;

namespace Buttr.Unity {
    internal static class CMDArgsBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() {
            CMDArgs.Initialize(Environment.GetCommandLineArgs());
        }
    }
}
