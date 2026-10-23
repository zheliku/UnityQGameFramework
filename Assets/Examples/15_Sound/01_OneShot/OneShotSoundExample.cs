#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Sound;
using UnityQGameFramework.Runtime;
using PlaySoundSuccessEventArgs = UnityQGameFramework.Runtime.PlaySoundSuccessEventArgs;
using PlaySoundFailureEventArgs = UnityQGameFramework.Runtime.PlaySoundFailureEventArgs;

namespace UnityQGameFramework.Examples
{
    public sealed class OneShotSoundExample : MonoBehaviour
    {
        [SerializeField] private string soundGuid;
        [SerializeField] private Dropdown playModeDropdown;
        [SerializeField] private Button playButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button cancelAllButton;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text outputText;

        private readonly List<int> loading = new List<int>();
        private ISoundAgent agent;
        private int serialId;
        private bool initialized;
        private bool cancelled;
        private string result = "";

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                // 等框架组件完成 Start；对象销毁时取消等待。
                await UniTask.NextFrame(cancellation);
                // 组音量也作用于不传播放参数的重载。
                QGF.Sound.AddSoundGroup("Prompt", false, false, 0.25f, 1);
                // 加载结果通过事件返回；序号用于区分当前请求与旧请求。
                QGF.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
                QGF.Event.Subscribe(PlaySoundFailureEventArgs.EventId, OnFailed);
                playModeDropdown.ClearOptions();
                playModeDropdown.AddOptions(new List<string>
                {
                    "资源、组", "资源、组、加载优先级", "资源、组、播放参数",
                    "资源、组、上下文", "资源、组、加载优先级、播放参数", "完整参数与上下文"
                });
                playButton.onClick.AddListener(Play);
                cancelButton.onClick.AddListener(() =>
                {
                    // 加载中停止：资源返回时释放它，不再启动代理。
                    cancelled = QGF.Sound.StopSound(serialId);
                    result = "已取消本次播放，资源将在后台返回后释放。";
                });
                cancelAllButton.onClick.AddListener(() =>
                {
                    QGF.Sound.StopAllLoadingSounds();
                    cancelled = true;
                    result = "已取消全部待播放请求，资源在后台收尾。";
                });
                initialized = true;
                Play();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Play()
        {
            string asset = UnityEditor.AssetDatabase.GUIDToAssetPath(soundGuid);
            cancelled = false;
            result = "";
            agent = null;
            // 返回序号；成功事件里才能取得代理。10 是加载优先级，与代理抢占优先级不同。
            switch (playModeDropdown.value)
            {
                case 0: serialId = QGF.Sound.PlaySound(asset, "Prompt"); break;
                case 1: serialId = QGF.Sound.PlaySound(asset, "Prompt", 10); break;
                case 2: serialId = QGF.Sound.PlaySound(asset, "Prompt", PlaySoundParams.Create()); break;
                case 3: serialId = QGF.Sound.PlaySound(asset, "Prompt", (object)this); break;
                case 4: serialId = QGF.Sound.PlaySound(asset, "Prompt", 10, PlaySoundParams.Create()); break;
                default: serialId = QGF.Sound.PlaySound(asset, "Prompt", 10, PlaySoundParams.Create(), this); break;
            }
            // 播放参数来自引用池，交给 PlaySound 后由框架回收。
        }

        private void OnPlayed(object sender, GameEventArgs args)
        {
            var e = (PlaySoundSuccessEventArgs)args;
            if (e.SerialId != serialId) return;
            agent = e.SoundAgent;
            result = "加载成功；UserData：" + (ReferenceEquals(e.UserData, this) ? "当前对象" : "未设置");
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var e = (PlaySoundFailureEventArgs)args;
            if (e.SerialId == serialId) result = $"播放失败：{e.ErrorCode}\n{e.ErrorMessage}";
        }

        private void Update()
        {
            if (!initialized) return;
            // 这里只查当前请求，不把正在加载误认为已经播放。
            bool pending = QGF.Sound.IsLoadingSound(serialId);
            // List 重载复用容器；另一个重载直接返回 int[]。
            QGF.Sound.GetAllLoadingSoundSerialIds(loading);
            bool playing = agent != null && agent.SerialId == serialId && agent.IsPlaying;
            float length = playing ? agent.Length : 0;
            progressSlider.SetValueWithoutNotify(length > 0 ? agent.Time / length : 0);
            outputText.text = $"播放序号：{serialId}    等待中的请求：{loading.Count}\n"
                + (pending ? (cancelled ? "取消收尾中" : "正在加载（本场景延迟 2 秒）")
                    : playing ? $"播放：{agent.Time:F1} / {length:F1} 秒" : cancelled ? "已取消" : "已结束")
                + "\n" + result;
            playButton.interactable = !pending && !playing;
            cancelButton.interactable = cancelAllButton.interactable = pending && !cancelled;
        }

        private void OnDestroy()
        {
            // 只给仍存在的组件解绑，框架可能已经退出。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(PlaySoundSuccessEventArgs.EventId, OnPlayed))
                    events.Unsubscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
                if (events.Check(PlaySoundFailureEventArgs.EventId, OnFailed))
                    events.Unsubscribe(PlaySoundFailureEventArgs.EventId, OnFailed);
            }
            var sound = GameEntry.GetComponent<SoundComponent>();
            if (sound != null && serialId != 0) sound.StopSound(serialId);
        }
    }
}
#endif
