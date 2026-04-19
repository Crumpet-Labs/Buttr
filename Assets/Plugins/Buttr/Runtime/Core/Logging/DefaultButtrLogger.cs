using System.Diagnostics;

namespace Buttr.Core {
    internal sealed class DefaultButtrLogger : IButtrLogger {
        public void Log(string message)        => Debug.WriteLine("[Buttr] " + message);
        public void LogWarning(string message) => Debug.WriteLine("[Buttr][warn] " + message);
        public void LogError(string message)   => Debug.WriteLine("[Buttr][error] " + message);
    }
}
