using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 按级别输出 QGF 日志。
    /// </summary>
    public sealed class LoggingExample : MonoBehaviour
    {
        [SerializeField] private TMP_InputField messageInput;
        [SerializeField] private Button debugButton;
        [SerializeField] private Button infoButton;
        [SerializeField] private Button warningButton;
        [SerializeField] private Button errorButton;
        [SerializeField] private TMP_Text statusText;
        private void Start()
        {

            // Conditional 按调用方程序集的宏决定是否保留调用；按钮显示本脚本的编译结果。
            debugButton.interactable = false;
            infoButton.interactable = false;
            warningButton.interactable = false;
            errorButton.interactable = false;
#if ENABLE_LOG || ENABLE_DEBUG_LOG || ENABLE_DEBUG_AND_ABOVE_LOG
            debugButton.interactable = true;
#endif
#if ENABLE_LOG || ENABLE_INFO_LOG || ENABLE_DEBUG_AND_ABOVE_LOG || ENABLE_INFO_AND_ABOVE_LOG
            infoButton.interactable = true;
#endif
#if ENABLE_LOG || ENABLE_WARNING_LOG || ENABLE_DEBUG_AND_ABOVE_LOG || ENABLE_INFO_AND_ABOVE_LOG || ENABLE_WARNING_AND_ABOVE_LOG
            warningButton.interactable = true;
#endif
#if ENABLE_LOG || ENABLE_ERROR_LOG || ENABLE_DEBUG_AND_ABOVE_LOG || ENABLE_INFO_AND_ABOVE_LOG || ENABLE_WARNING_AND_ABOVE_LOG || ENABLE_ERROR_AND_ABOVE_LOG
            errorButton.interactable = true;
#endif
            statusText.text = $"Log levels: Debug {(debugButton.interactable ? "Available" : "Not compiled")} / "
                + $"Info {(infoButton.interactable ? "Available" : "Not compiled")} / "
                + $"Warning {(warningButton.interactable ? "Available" : "Not compiled")} / "
                + $"Error {(errorButton.interactable ? "Available" : "Not compiled")}";
            // 输出到 Console。未编译的级别不可点击，不替宿主项目开启日志宏。
            debugButton.onClick.AddListener(() => Log.Debug(messageInput.text));
            infoButton.onClick.AddListener(() => Log.Info(messageInput.text));
            warningButton.onClick.AddListener(() => Log.Warning(messageInput.text));
            errorButton.onClick.AddListener(() => Log.Error(messageInput.text));
        }
    }
}
