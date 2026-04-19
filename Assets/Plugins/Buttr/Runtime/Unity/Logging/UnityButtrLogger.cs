using Buttr.Core;
using UnityEngine;

namespace Buttr.Unity {
    internal sealed class UnityButtrLogger : IButtrLogger {
        public void Log(string message)        => Debug.Log(message);
        public void LogWarning(string message) => Debug.LogWarning(message);
        public void LogError(string message)   => Debug.LogError(message);
    }
}
