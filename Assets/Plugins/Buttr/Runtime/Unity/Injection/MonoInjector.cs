using System;
using Buttr.Injection;
using UnityEngine;

namespace Buttr.Unity.Injection {
    [DefaultExecutionOrder(-9000)]
    internal sealed class MonoInjector : MonoBehaviour {
        [SerializeField] private MonoInjectStrategy m_InjectStrategy = MonoInjectStrategy.GameObjectAndChildren;
        [SerializeField, Tooltip(BehaviourInjectorTooltips.BEHAVIOUR_TOOLTIP)] private MonoBehaviour m_Behaviour;

        private void Awake() {
            if (m_Behaviour == null) {
                m_Behaviour = gameObject.GetComponent<MonoBehaviour>();
            }

            switch (m_InjectStrategy) {
                case MonoInjectStrategy.Mono:
                    InjectionProcessor.Inject(m_Behaviour);
                    break;
                case MonoInjectStrategy.GameObject:
                    InjectionProcessorUnityExtensions.InjectGameObject(m_Behaviour);
                    break;
                case MonoInjectStrategy.GameObjectAndChildren:
                    InjectionProcessorUnityExtensions.InjectSelfAndChildren(m_Behaviour);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Somehow... Incredibly... you managed to trigger this... 👍");
            }

            Destroy(this);
        }
    }
}
