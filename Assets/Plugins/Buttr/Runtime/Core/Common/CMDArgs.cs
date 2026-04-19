using System.Collections.Generic;

namespace Buttr.Core {
    /// <summary>
    /// A simple static store for command-line-style key/value arguments.
    /// </summary>
    /// <remarks>
    /// The engine-specific bootstrap is responsible for calling <see cref="Initialize"/> once with the
    /// process's argument list. Until then, the store behaves as if initialised with an empty list.
    /// Pairs are read sequentially: <c>key1, value1, key2, value2, ...</c>. A trailing unpaired key
    /// is stored with <see cref="string.Empty"/> as its value.
    /// </remarks>
    public static class CMDArgs {
        private static IDictionary<string, string> s_Args = new Dictionary<string, string>();

        public static void Initialize(IEnumerable<string> args) {
            s_Args = BuildDictionary(args);
        }

        public static IDictionary<string, string> Read() {
            return s_Args;
        }

        public static bool Exists(string arg) {
            return s_Args.ContainsKey(arg);
        }

        public static bool TryGetValue(string key, out string value) {
            return s_Args.TryGetValue(key, out value);
        }

        private static Dictionary<string, string> BuildDictionary(IEnumerable<string> args) {
            var result = new Dictionary<string, string>();
            if (args == null) return result;

            string pendingKey = null;
            foreach (var arg in args) {
                if (pendingKey == null) {
                    pendingKey = arg;
                }
                else {
                    result[pendingKey] = arg;
                    pendingKey = null;
                }
            }
            if (pendingKey != null) result[pendingKey] = string.Empty;
            return result;
        }
    }
}
