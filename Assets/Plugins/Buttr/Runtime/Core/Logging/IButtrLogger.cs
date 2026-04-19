namespace Buttr.Core {
    public interface IButtrLogger {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
}
