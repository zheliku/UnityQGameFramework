#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Localization;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class LoadDictionary : MonoBehaviour
    {
        [SerializeField] private TextAsset dictionary, invalidDictionary;
        [SerializeField] private Dropdown loadMode, languageMode;
        [SerializeField] private Toggle failureToggle;
        [SerializeField] private Button loadButton;
        [SerializeField] private Text stateText, contextText, languageText;
        private string pendingPath;
        private bool ready, loading;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // ReadData 通过资源模块异步读取；先订阅，再发起加载。
                QGF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLoaded);
                QGF.Event.Subscribe(LoadDictionaryFailureEventArgs.EventId, OnFailed);
                loadButton.onClick.AddListener(() =>
                {
                    QGF.Localization.RemoveAllRawStrings();
                    QGF.Localization.Language = languageMode.value == 0 ? Language.ChineseSimplified : Language.English;
                    pendingPath = UnityEditor.AssetDatabase.GetAssetPath(failureToggle.isOn ? invalidDictionary : dictionary);
                    loading = true;
                    stateText.text = "读取中…";
                    contextText.text = string.Empty;
                    switch (loadMode.value)
                    {
                        case 0: QGF.Localization.ReadData(pendingPath); break;
                        case 1: QGF.Localization.ReadData(pendingPath, 10); break;
                        case 2: QGF.Localization.ReadData(pendingPath, this); break;
                        default: QGF.Localization.ReadData(pendingPath, 10, this); break;
                    }
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void OnLoaded(object sender, GameEventArgs args)
        {
            var e = (LoadDictionarySuccessEventArgs)args;
            if (e.DictionaryAssetName != pendingPath) return;
            // 复制回调里的结果；事件参数在派发后会回收。
            stateText.text = $"{QGF.Localization.GetString("Game.Name")}\n读取用时 {e.Duration:F2} 秒";
            contextText.text = ReferenceEquals(e.UserData, this) ? "UserData：当前对象" : "UserData：未设置";
            loading = false;
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var e = (LoadDictionaryFailureEventArgs)args;
            if (e.DictionaryAssetName != pendingPath) return;
            stateText.text = "读取失败：" + e.ErrorMessage;
            contextText.text = ReferenceEquals(e.UserData, this) ? "失败回调，UserData：当前对象" : "失败回调，UserData：未设置";
            loading = false;
        }

        private void Update()
        {
            if (!ready) return;
            loadButton.interactable = loadMode.interactable = languageMode.interactable = failureToggle.interactable = !loading;
            languageText.text = $"当前 {QGF.Localization.Language}    系统 {QGF.Localization.SystemLanguage}\n字典 {QGF.Localization.DictionaryCount} 项";
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events == null) return;
            if (events.Check(LoadDictionarySuccessEventArgs.EventId, OnLoaded)) events.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLoaded);
            if (events.Check(LoadDictionaryFailureEventArgs.EventId, OnFailed)) events.Unsubscribe(LoadDictionaryFailureEventArgs.EventId, OnFailed);
        }
    }
}
#endif
