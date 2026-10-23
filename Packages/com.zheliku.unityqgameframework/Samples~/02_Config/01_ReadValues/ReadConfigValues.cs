using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ReadConfigValues : MonoBehaviour
    {
        [SerializeField] private InputField tomlInput;
        [SerializeField] private Button parseButton;
        [SerializeField] private Dropdown modeDropdown;
        [SerializeField] private Text resultText;
        [SerializeField] private Text countText;
        [SerializeField] private Text valuesText;
        [SerializeField] private Text defaultsText;
        private bool viewReady;
        [SerializeField] private TextAsset config;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                viewReady = true;

                tomlInput.text = config.text;
                modeDropdown.ClearOptions();
                modeDropdown.AddOptions(new List<string> { "字符串", "字符串 + 上下文", "UTF-8 字节", "UTF-8 字节 + 上下文", "字节区间", "字节区间 + 上下文" });
                // 先读取初始 TOML，界面可以直接修改这份文本。
                QGF.Config.ParseData(config.text);
                parseButton.onClick.AddListener(() =>
                {
                    // ParseData 会合并键值；本场景单独使用 Config，重读前先清空。
                    QGF.Config.RemoveAllConfigs();
                    string text = tomlInput.text;
                    byte[] bytes = Encoding.UTF8.GetBytes(text);
                    byte[] packet = new byte[bytes.Length + 8];
                    // 区间重载只解析正文；长度使用 UTF-8 字节数。
                    Array.Copy(bytes, 0, packet, 4, bytes.Length);
                    bool parsed = false;
                    switch (modeDropdown.value)
                    {
                        case 0: parsed = QGF.Config.ParseData(text); break;
                        case 1: parsed = QGF.Config.ParseData(text, this); break;
                        case 2: parsed = QGF.Config.ParseData(bytes); break;
                        case 3: parsed = QGF.Config.ParseData(bytes, this); break;
                        case 4: parsed = QGF.Config.ParseData(packet, 4, bytes.Length); break;
                        case 5: parsed = QGF.Config.ParseData(packet, 4, bytes.Length, this); break;
                    }
                    resultText.text = parsed ? $"已读取 {bytes.Length} 字节正文" : "TOML 格式有误";
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!viewReady) return;
            // Count 返回当前配置数量。
            countText.text = $"当前配置：{QGF.Config.Count} 项";
            // 不带默认值的 Get 要求键存在。未加载或用户删掉字段时，界面显示缺失。
            valuesText.text = QGF.Config.HasConfig("Game.Title") && QGF.Config.HasConfig("Player.MaxHp")
                && QGF.Config.HasConfig("Player.Speed") && QGF.Config.HasConfig("Game.FeatureEnabled")
                ? $"游戏：{QGF.Config.GetString("Game.Title")}\n生命：{QGF.Config.GetInt("Player.MaxHp")}\n"
                    + $"速度：{QGF.Config.GetFloat("Player.Speed")}\n功能开关：{(QGF.Config.GetBool("Game.FeatureEnabled") ? "开启" : "关闭")}"
                : "必需配置不完整，请补齐上面的四个键。";
            // 可选项使用默认值。把 Audio.Volume 写进输入框，比较缺省与配置值。
            defaultsText.text = $"可选音量：{QGF.Config.GetFloat("Audio.Volume", 0.5f):P0}\n"
                + $"静音：{(QGF.Config.GetBool("Audio.Mute", false) ? "开启" : "关闭")}    音轨：{QGF.Config.GetInt("Audio.Track", 1)}\n"
                + $"音轨名称：{QGF.Config.GetString("Audio.Name", "默认音轨")}";
        }
    }
}
