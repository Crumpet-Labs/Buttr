using UnityEngine;

namespace Buttr.Unity.Injection {
    [DefaultExecutionOrder(-9000)]
    internal sealed class SceneInjector : MonoBehaviour {
        private void Awake() {
            InjectionProcessorUnityExtensions.InjectScene(gameObject.scene);
            Destroy(this);
        }
    }
}
