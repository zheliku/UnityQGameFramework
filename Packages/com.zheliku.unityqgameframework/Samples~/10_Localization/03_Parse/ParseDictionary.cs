using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Localization;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ParseDictionary : MonoBehaviour
    {
        [SerializeField] private TextAsset dictionary;
        [SerializeField] private Dropdown inputMode;
        [SerializeField] private InputField keyInput;
        [SerializeField] private Button parseButton, removeButton, clearButton, reserveButton, freeButton;
        [SerializeField] private Text stateText, rawText, cacheText;
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // 查询跟随这份字典的语言设置。
                QGF.Localization.Language = Language.ChineseSimplified;
                parseButton.onClick.AddListener(Parse);
                removeButton.onClick.AddListener(() => QGF.Localization.RemoveRawString(keyInput.text));
                clearButton.onClick.AddListener(() => QGF.Localization.RemoveAllRawStrings());
                // 预留或释放 Localization 复用的字节读取缓冲。
                reserveButton.onClick.AddListener(() => QGF.Localization.EnsureCachedBytesSize(4096));
                freeButton.onClick.AddListener(() => QGF.Localization.FreeCachedBytes());
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Parse()
        {
            var localization = QGF.Localization;
            // 本场景独立重读整份字典，先移除旧键，避免重复添加。
            localization.RemoveAllRawStrings();
            byte[] bytes = Encoding.UTF8.GetBytes(dictionary.text);
            var segment = new byte[bytes.Length + 8];
            Array.Copy(bytes, 0, segment, 4, bytes.Length);
            bool parsed;
            // ParseData 同步解析；userData 交给 Helper，不会产生异步加载事件。
            switch (inputMode.value)
            {
                case 0: parsed = localization.ParseData(dictionary.text); break;
                case 1: parsed = localization.ParseData(dictionary.text, this); break;
                case 2: parsed = localization.ParseData(bytes); break;
                case 3: parsed = localization.ParseData(bytes, this); break;
                case 4: parsed = localization.ParseData(segment, 4, bytes.Length); break;
                default: parsed = localization.ParseData(segment, 4, bytes.Length, this); break;
            }
            stateText.text = parsed ? "解析完成，可查询或移除键。" : "解析失败。";
        }

        private void Update()
        {
            if (!ready) return;
            // HasRawString 用来禁用对不存在键的删除操作。
            bool exists = !string.IsNullOrEmpty(keyInput.text) && QGF.Localization.HasRawString(keyInput.text);
            removeButton.interactable = exists;
            clearButton.interactable = QGF.Localization.DictionaryCount > 0;
            // GetRawString 保留占位符，GetString 才负责填入格式化参数。
            rawText.text = exists ? QGF.Localization.GetRawString(keyInput.text) : "当前字典中没有这个键。";
            cacheText.text = $"字典 {QGF.Localization.DictionaryCount} 项    共享读取缓冲 {QGF.Localization.CachedBytesSize} 字节";
        }
    }
}
