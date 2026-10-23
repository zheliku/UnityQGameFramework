using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class SaveSettingObject : MonoBehaviour
    {
        [SerializeField] private InputField nameInput;
        [SerializeField] private Slider levelSlider;
        [SerializeField] private Dropdown writeMode, readMode;
        [SerializeField] private Button saveButton, readButton, removeButton;
        [SerializeField] private Text stateText, resultText;
        private const string Key = "Pilot";
        private readonly Pilot fallback = new Pilot { Name = "新飞行员", Level = 1 };
        private Pilot loaded;
        private bool ready;

        [Serializable]
        public sealed class Pilot
        {
            // 默认 JSON Helper 保存字段；GetObject 会从保存的 JSON 重建对象。
            public string Name;
            public int Level;
        }

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                levelSlider.wholeNumbers = true;
                saveButton.onClick.AddListener(() =>
                {
                    var pilot = new Pilot { Name = nameInput.text, Level = (int)levelSlider.value };
                    // 默认 JSON Helper 序列化字段；设置中保存序列化结果。
                    if (writeMode.value == 0) QGF.Setting.SetObject<Pilot>(Key, pilot);
                    else QGF.Setting.SetObject(Key, (object)pilot);
                    QGF.Setting.Save();
                    resultText.text = $"已保存 {pilot.Name}，等级 {pilot.Level}。";
                });
                readButton.onClick.AddListener(() =>
                {
                    // 缺键时直接返回 fallback 本身；有键时反序列化为新的 Pilot。
                    switch (readMode.value)
                    {
                        case 0: loaded = QGF.Setting.GetObject<Pilot>(Key); break;
                        case 1: loaded = (Pilot)QGF.Setting.GetObject(typeof(Pilot), Key); break;
                        case 2: loaded = QGF.Setting.GetObject(Key, fallback); break;
                        default: loaded = (Pilot)QGF.Setting.GetObject(typeof(Pilot), Key, fallback); break;
                    }
                    resultText.text = $"{(ReferenceEquals(loaded, fallback) ? "默认对象" : "读取对象")}：{loaded.Name}，等级 {loaded.Level}。";
                });
                removeButton.onClick.AddListener(() =>
                {
                    QGF.Setting.RemoveSetting(Key);
                    QGF.Setting.Save();
                    loaded = null;
                    resultText.text = "已移除 Pilot，可用默认值重载读取。";
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!ready) return;
            bool exists = QGF.Setting.HasSetting(Key);
            readButton.interactable = exists || readMode.value >= 2;
            removeButton.interactable = exists;
            stateText.text = $"待保存等级 {(int)levelSlider.value}    Pilot {(exists ? "已存在" : "未创建")}";
        }
    }
}
