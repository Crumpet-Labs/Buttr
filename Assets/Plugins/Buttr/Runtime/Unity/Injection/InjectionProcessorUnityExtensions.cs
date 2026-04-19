using Buttr.Injection;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Buttr.Unity.Injection {
    /// <summary>
    /// Scene / GameObject / hierarchy-walking helpers that drive <see cref="InjectionProcessor.Inject"/>
    /// over Unity types. Kept separate from the engine-agnostic <c>InjectionProcessor</c> so that
    /// <c>Buttr.Injection</c> has no Unity dependency.
    /// </summary>
    public static class InjectionProcessorUnityExtensions {
        public static void InjectScene(Scene scene) {
            foreach (var root in scene.GetRootGameObjects()) {
                InjectSelfAndChildren(root.GetComponent<MonoBehaviour>());
            }
        }

        public static void InjectActiveScene() {
            InjectScene(SceneManager.GetActiveScene());
        }

        public static void InjectAllLoadedScenes() {
            for (var i = 0; i < SceneManager.sceneCount; i++) {
                InjectScene(SceneManager.GetSceneAt(i));
            }
        }

        public static void InjectSelfAndChildren(object instance) {
            if (instance == null) throw new InjectionException("Cannot inject into a null instance");
            if (instance is not MonoBehaviour mono)
                throw new InjectionException("Can only inject into MonoBehaviour Instances through attribute injection");

            var buffer = ListPool<MonoBehaviour>.Get();
            mono.GetComponentsInChildren(true, buffer);

            foreach (var mb in buffer) {
                if (mb is IInjectable injectable)
                    InjectionProcessor.Inject(injectable);
            }

            ListPool<MonoBehaviour>.Release(buffer);
        }

        public static void InjectGameObject(object instance) {
            if (instance == null) throw new InjectionException("Cannot inject into a null instance");
            if (instance is not MonoBehaviour mono)
                throw new InjectionException("Can only inject into MonoBehaviour Instances through attribute injection");

            var buffer = ListPool<MonoBehaviour>.Get();
            buffer.AddRange(mono.gameObject.GetComponents<MonoBehaviour>());

            foreach (var mb in buffer) {
                if (mb is IInjectable injectable)
                    InjectionProcessor.Inject(injectable);
            }

            ListPool<MonoBehaviour>.Release(buffer);
        }
    }
}
