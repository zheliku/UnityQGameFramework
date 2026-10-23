using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Setting;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class SaveCounter : MonoBehaviour
    {
        [SerializeField] private InputField nameInput;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Button resetButton, saveButton, reloadButton, resetVolumeButton;
        [SerializeField] private Text countText, valuesText, resultText;
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // 首次运行用默认值；Set 修改内存，Save 请求写入设置文件。
                int count = QGF.Setting.GetInt("LaunchCount", 0) + 1;
                QGF.Setting.SetInt("LaunchCount", count);
                QGF.Setting.Save();
                nameInput.text = QGF.Setting.GetString("PlayerName", "飞行员");
                muteToggle.isOn = QGF.Setting.GetBool("Muted", false);
                volumeSlider.value = QGF.Setting.GetFloat("Volume", .5f);
                saveButton.onClick.AddListener(() =>
                {
                    QGF.Setting.SetString("PlayerName", nameInput.text);
                    QGF.Setting.SetBool("Muted", muteToggle.isOn);
                    QGF.Setting.SetFloat("Volume", volumeSlider.value);
                    QGF.Setting.Save();
                    resultText.text = "已保存姓名、静音和音量。";
                });
                reloadButton.onClick.AddListener(() =>
                {
                    // 组件在启动时自动 Load；手动重读使用同一份 Core 管理器。
                    if (!FrameworkEntry.GetModule<ISettingManager>().Load())
                    {
                        resultText.text = "读取失败，请检查设置文件。";
                        return;
                    }
                    nameInput.SetTextWithoutNotify(QGF.Setting.GetString("PlayerName", "飞行员"));
                    muteToggle.SetIsOnWithoutNotify(QGF.Setting.GetBool("Muted", false));
                    volumeSlider.SetValueWithoutNotify(QGF.Setting.GetFloat("Volume", .5f));
                    resultText.text = "已重新读取，输入框与当前设置同步。";
                });
                resetButton.onClick.AddListener(() =>
                {
                    QGF.Setting.RemoveSetting("LaunchCount");
                    QGF.Setting.Save();
                    resultText.text = "已移除运行计数，其他设置保留。";
                });
                resetVolumeButton.onClick.AddListener(() =>
                {
                    QGF.Setting.RemoveSetting("Volume");
                    volumeSlider.value = QGF.Setting.GetFloat("Volume", .5f);
                    QGF.Setting.Save();
                    resultText.text = "已移除音量，恢复默认值。";
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!ready) return;
            // 无默认值的重载在键存在时读取；缺键分支在界面中标为“默认”。
            int count = QGF.Setting.HasSetting("LaunchCount") ? QGF.Setting.GetInt("LaunchCount") : 0;
            string name = QGF.Setting.HasSetting("PlayerName") ? QGF.Setting.GetString("PlayerName") : "飞行员（默认）";
            bool muted = QGF.Setting.HasSetting("Muted") && QGF.Setting.GetBool("Muted");
            float volume = QGF.Setting.HasSetting("Volume") ? QGF.Setting.GetFloat("Volume") : .5f;
            countText.text = $"运行 {count} 次    设置 {QGF.Setting.Count} 项";
            valuesText.text = $"当前设置：{name} · 静音 {(muted ? "开" : "关")} · 音量 {volume:P0}\n"
                + $"输入框：{nameInput.text} · 静音 {(muteToggle.isOn ? "开" : "关")} · 音量 {volumeSlider.value:P0}";
        }
    }
}
