using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ApplicationSettingsExample : MonoBehaviour
    {
        [SerializeField] private Text fpsText;
        [SerializeField] private Button fps30Button;
        [SerializeField] private Button fps60Button;
        [SerializeField] private Button unlimitedButton;
        [SerializeField] private Toggle backgroundToggle;
        [SerializeField] private Toggle neverSleepToggle;
        private bool ready;
        private int originalFrameRate;
        private bool originalBackground, originalNeverSleep;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);

                // 先记下宿主设置；离开场景时恢复，避免影响其他示例。
                originalFrameRate = QGF.Base.FrameRate;
                originalBackground = QGF.Base.RunInBackground;
                originalNeverSleep = QGF.Base.NeverSleep;
                // FrameRate 设置目标值；实际帧率还受垂直同步和平台限制影响。
                fps30Button.onClick.AddListener(() => QGF.Base.FrameRate = 30);
                fps60Button.onClick.AddListener(() => QGF.Base.FrameRate = 60);
                unlimitedButton.onClick.AddListener(() => QGF.Base.FrameRate = -1);
                var background = backgroundToggle;
                background.isOn = originalBackground;
                // 失去焦点后是否继续运行。
                background.onValueChanged.AddListener(value => QGF.Base.RunInBackground = value);
                var sleep = neverSleepToggle;
                sleep.isOn = originalNeverSleep;
                // NeverSleep 禁止设备休眠，不会暂停游戏时间。
                sleep.onValueChanged.AddListener(value => QGF.Base.NeverSleep = value);
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (ready) fpsText.text =
                $"目标帧率：{QGF.Base.FrameRate}    当前：{1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f):F0}";
        }

        private void OnDestroy()
        {
            if (!ready) return;
            // 组件属性同时更新记录和 Unity 参数；框架先退出时才直接恢复 Unity。
            var framework = GameEntry.GetComponent<BaseComponent>();
            if (framework != null)
            {
                framework.FrameRate = originalFrameRate;
                framework.RunInBackground = originalBackground;
                framework.NeverSleep = originalNeverSleep;
            }
            else
            {
                Application.targetFrameRate = originalFrameRate;
                Application.runInBackground = originalBackground;
                Screen.sleepTimeout = originalNeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }
    }
}
