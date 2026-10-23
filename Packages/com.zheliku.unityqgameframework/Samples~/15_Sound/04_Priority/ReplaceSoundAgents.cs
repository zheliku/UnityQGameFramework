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
    public sealed class ReplaceSoundAgents : MonoBehaviour
    {
        [SerializeField] private string soundGuid;
        [SerializeField] private Dropdown priorityDropdown;
        [SerializeField] private Toggle protectToggle;
        [SerializeField] private Button playButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Text outputText;

        private readonly List<ISoundAgent> agents = new List<ISoundAgent>();
        private readonly List<int> requests = new List<int>();
        private ISoundGroup group;
        private bool initialized;
        private string result = "先播放两次填满代理，再试不同优先级。";

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                // 先让框架完成初始化，再创建本示例的声音组。
                await UniTask.NextFrame(cancellation);
                // 两个代理就是最多两路声音；第三次播放需要抢占或被拒绝。
                QGF.Sound.AddSoundGroup("Effects", false, false, 0.25f, 2);
                group = QGF.Sound.GetSoundGroup("Effects");
                // 成功拿到代理；容量已满且不能抢占时走失败事件。
                QGF.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
                QGF.Event.Subscribe(PlaySoundFailureEventArgs.EventId, OnFailed);
                priorityDropdown.ClearOptions();
                priorityDropdown.AddOptions(new List<string> { "低：0", "中：10", "高：20" });
                // 保护同级不会阻止更高优先级抢占，只拒绝同级替换。
                protectToggle.onValueChanged.AddListener(value => group.AvoidBeingReplacedBySamePriority = value);
                playButton.onClick.AddListener(Play);
                // 停止组内全部循环音，两个代理回到空闲状态。
                clearButton.onClick.AddListener(() => group.StopAllLoadedSounds());
                initialized = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Play()
        {
            // PlaySound 接管这份池化参数。
            var parameters = PlaySoundParams.Create();
            parameters.Loop = true;
            parameters.VolumeInSoundGroup = 0.5f;
            // Priority 决定代理抢占：数值越大越优先；方法参数中的 0 只是资源加载优先级。
            parameters.Priority = priorityDropdown.value * 10;
            // 记住自己的请求序号；代理被复用后不能据旧代理引用判断播放归属。
            requests.Add(QGF.Sound.PlaySound(UnityEditor.AssetDatabase.GUIDToAssetPath(soundGuid), "Effects", 0, parameters, this));
        }

        private void OnPlayed(object sender, GameEventArgs args)
        {
            var e = (PlaySoundSuccessEventArgs)args;
            if (!ReferenceEquals(e.UserData, this)) return;
            if (!agents.Contains(e.SoundAgent)) agents.Add(e.SoundAgent);
            result = $"序号 {e.SerialId} 已播放，优先级 {e.SoundAgent.Priority}。";
            // 代理会复用，访问前用 SerialId 确认所属播放。
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var e = (PlaySoundFailureEventArgs)args;
            if (ReferenceEquals(e.UserData, this)) result = $"序号 {e.SerialId} 被拒绝：{e.ErrorCode}";
        }

        private void Update()
        {
            if (!initialized) return;
            string protection = group.AvoidBeingReplacedBySamePriority ? "开启" : "关闭";
            outputText.text = $"代理容量：{group.SoundAgentCount}    保护同优先级：{protection}\n";
            for (int index = 0; index < 2; index++)
            {
                var agent = index < agents.Count ? agents[index] : null;
                outputText.text += agent != null && agent.Length > 0
                    ? $"代理 {index + 1}：序号 {agent.SerialId} / 优先级 {agent.Priority} / {agent.Time:F1} 秒\n"
                    : $"代理 {index + 1}：空闲\n";
            }
            outputText.text += result;
            // 数组重载直接用于判断：仍有请求加载时先禁用播放按钮。
            playButton.interactable = QGF.Sound.GetAllLoadingSoundSerialIds().Length == 0;
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
            // StopSound 同时处理加载和播放状态，已结束的序号不会影响其他播放。
            var sound = GameEntry.GetComponent<SoundComponent>();
            if (sound != null)
                foreach (int id in requests) sound.StopSound(id);
        }
    }
}
#endif
