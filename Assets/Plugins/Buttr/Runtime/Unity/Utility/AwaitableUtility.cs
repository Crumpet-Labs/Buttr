using UnityEngine;

namespace Buttr.Core {
    public static partial class AwaitableUtility {
        public static Awaitable<T> FromResult<T>(T result) {
            var acs = new AwaitableCompletionSource<T>();
            acs.SetResult(result);
            return acs.Awaitable;
        }

        public static Awaitable CompletedTask {
            get {
                var acs = new AwaitableCompletionSource();
                acs.SetResult();
                return acs.Awaitable;
            }
        }
    }
}
