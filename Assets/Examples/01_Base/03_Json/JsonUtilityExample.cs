using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class JsonUtilityExample : MonoBehaviour
    {
        [SerializeField] private InputField nameInput;
        [SerializeField] private Text levelText;
        [SerializeField] private Button levelUpButton;
        [SerializeField] private Button serializeButton;
        [SerializeField] private InputField jsonInput;
        [SerializeField] private Dropdown readMode;
        [SerializeField] private Button deserializeButton;
        [SerializeField] private Text resultText;
        [SerializeField] private TextAsset json;
        private PlayerSnapshot player;

        // 默认 JsonUtility 序列化字段，不序列化普通 C# 属性。
        [Serializable]
        public sealed class PlayerSnapshot
        {
            public string Name;
            public int Level;
        }

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);

                // Base 已安装默认 JSON Helper；json 是场景引用的 TextAsset。
                player = Utility.Json.ToObject<PlayerSnapshot>(json.text);
                nameInput.text = player.Name;
                jsonInput.text = Utility.Json.ToJson(player);
                resultText.text = "已从 Player.json 读取初始角色。";
                readMode.ClearOptions();
                readMode.AddOptions(new List<string> { "泛型 ToObject<T>", "类型 ToObject(Type, json)" });
                nameInput.onValueChanged.AddListener(value => player.Name = value);
                levelUpButton.onClick.AddListener(() => player.Level++);
                // ToJson 返回序列化后的字符串。
                serializeButton.onClick.AddListener(() =>
                {
                    jsonInput.text = Utility.Json.ToJson(player);
                    resultText.text = "已将当前角色写入 JSON。";
                });
                deserializeButton.onClick.AddListener(() =>
                {
                    try
                    {
                        // 已知类型用泛型；运行时才确定类型时传 Type，返回值需要转换。
                        var next = readMode.value == 0
                            ? Utility.Json.ToObject<PlayerSnapshot>(jsonInput.text)
                            : (PlayerSnapshot)Utility.Json.ToObject(typeof(PlayerSnapshot), jsonInput.text);
                        if (next == null)
                        {
                            resultText.text = "请输入 JSON 对象，角色保持原值。";
                            return;
                        }
                        player = next;
                        nameInput.SetTextWithoutNotify(player.Name);
                        resultText.text = "已将 JSON 写回角色。";
                    }
                    // Utility.Json 会包装解析异常；输入有误时先保留原对象。
                    catch (FrameworkException exception) when (exception.InnerException is ArgumentException)
                    {
                        resultText.text = "JSON 格式有误，角色保持原值。";
                    }
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (player != null) levelText.text = $"等级：{player.Level}";
        }
    }
}
