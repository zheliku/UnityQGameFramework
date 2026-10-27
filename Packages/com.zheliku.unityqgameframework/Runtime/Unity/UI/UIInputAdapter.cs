using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 为独立 uGUI 场景选择当前项目启用的输入模块。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EventSystem))]
    [AddComponentMenu("Unity Q Game Framework/UI Input Adapter")]
    public sealed class UIInputAdapter : MonoBehaviour
    {
        /// <summary>
        /// 为事件系统配置当前输入后端所需的输入模块。
        /// </summary>
        private void Awake()
        {
            // 已经配置过输入模块的场景继续使用自己的设置。
            if (GetComponent<BaseInputModule>() != null)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            // Input System 是可选包，框架不强制引用它的程序集。
            var type = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (type == null)
            {
                Log.Error("Input System is enabled, but InputSystemUIInputModule could not be loaded.");
                return;
            }

            gameObject.AddComponent(type);
#else
            gameObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
