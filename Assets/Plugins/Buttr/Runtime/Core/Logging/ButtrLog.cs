using System.Threading;

namespace Buttr.Core {
    public static class ButtrLog {
        private static IButtrLogger s_Logger = new DefaultButtrLogger();

        public static void SetLogger(IButtrLogger logger) {
            Interlocked.Exchange(ref s_Logger, logger ?? new DefaultButtrLogger());
        }

        public static void Log(string message)        => Volatile.Read(ref s_Logger).Log(message);
        public static void LogWarning(string message) => Volatile.Read(ref s_Logger).LogWarning(message);
        public static void LogError(string message)   => Volatile.Read(ref s_Logger).LogError(message);
    }
}
