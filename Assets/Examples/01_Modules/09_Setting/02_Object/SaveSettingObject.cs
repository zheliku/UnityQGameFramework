using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示保存与读取对象。
    /// </summary>
    public sealed class SaveSettingObject : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField nameInput;
        [SerializeField]
        private Slider levelSlider;
        [SerializeField]
        private Button saveButton, readButton, removeButton;
        [SerializeField]
        private TMP_Text stateText, resultText;
        private const string Key = "Pilot";
        private readonly Pilot fallback = new Pilot
        {
            Name = "New Pilot",
            Level = 1
        };
        private Pilot loaded;
        private bool ready;

        /// <summary>
        /// 需要保存的飞行员数据。
        /// </summary>
        [Serializable]
        public sealed class Pilot
        {
            // 默认 JSON Helper 保存字段；GetObject 会从保存的 JSON 重建对象。
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
            levelSlider.wholeNumbers = true;
            saveButton.onClick.AddListener(() =>
            {
                var pilot = new Pilot
                {
                    Name = nameInput.text,
                    Level = (int)levelSlider.value
                };
                // 默认 JSON Helper 序列化字段；设置中保存序列化结果。
                QGF.Setting.SetObject<Pilot>(Key, pilot);
                resultText.text = QGF.Setting.Save() ? $"Saved {pilot.Name}; level {pilot.Level}." : "Save failed. Try again.";
            });
            readButton.onClick.AddListener(() =>
            {
                // 缺键时直接返回 fallback 本身；有键时反序列化为新的 Pilot。
                loaded = QGF.Setting.GetObject(Key, fallback);
                resultText.text = $"{(ReferenceEquals(loaded, fallback) ? "Default Object" : "Read Object")}：{loaded.Name}; level {loaded.Level}。";
            });
            removeButton.onClick.AddListener(() =>
            {
                QGF.Setting.RemoveSetting(Key);
                loaded = null;
                resultText.text = QGF.Setting.Save() ? "Pilot removed. Read it with the default-value overload." : "Save failed. Try again.";
            });
            ready = true;
        }

        private void Update()
        {
            if (!ready)
            {
                return;
            }

            bool exists = QGF.Setting.HasSetting(Key);
            readButton.interactable = true;
            removeButton.interactable = exists;
            stateText.text = $"Level to save {(int)levelSlider.value}    Pilot {(exists ? "Exists" : "Not created")}";
        }
    }
}
