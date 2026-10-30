using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示JSON 对象。
    /// </summary>
    public sealed class JsonUtilityExample : MonoBehaviour
    {
        [SerializeField] private TMP_InputField nameInput, jsonInput;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Button levelUpButton, serializeButton, deserializeButton;
        [SerializeField] private TextAsset json;
        private PlayerSnapshot player;

        /// <summary>
        /// 用于 JSON 转换的角色数据。
        /// </summary>
        [Serializable]
        public sealed class PlayerSnapshot
        {
            /// <summary>
            /// 名称。
            /// </summary>
            public string Name;

            /// <summary>
            /// 等级。
            /// </summary>
            public int Level;
        }

        private void Start()
        {
            // 默认 JSON Helper 读写字段；json 是 Inspector 指定的初始数据。
            player = Utility.Json.ToObject<PlayerSnapshot>(json.text);
            nameInput.text = player.Name;
            jsonInput.text = json.text;
            // JSON 输入框允许手动修改；“从 JSON 读取”会把修改后的快照还原到对象。
            jsonInput.readOnly = false;
            nameInput.onValueChanged.AddListener(value => player.Name = value);
            levelUpButton.onClick.AddListener(() => player.Level++);
            serializeButton.onClick.AddListener(() => jsonInput.text = Utility.Json.ToJson(player));
            deserializeButton.onClick.AddListener(() =>
            {
                // 读回上次序列化的快照，恢复姓名和等级。
                player = Utility.Json.ToObject<PlayerSnapshot>(jsonInput.text);
                nameInput.SetTextWithoutNotify(player.Name);
            });
        }

        private void Update()
        {
            levelText.text = $"Level: {player.Level}";
        }
    }
}
