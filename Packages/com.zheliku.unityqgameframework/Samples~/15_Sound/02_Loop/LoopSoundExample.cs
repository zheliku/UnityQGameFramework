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

namespace UnityQGameFramework.Examples
{
    public sealed class LoopSoundExample : MonoBehaviour
    {
        [SerializeField] private string soundGuid;
        [SerializeField] private Dropdown fadeDropdown, controlDropdown;
        [SerializeField] private Toggle loopToggle;
        [SerializeField] private Slider pitchSlider;
        [SerializeField] private Slider seekSlider;
        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button replayButton, resetButton;
        [SerializeField] private Text outputText;

        private ISoundAgent agent;
        private int serialId;
        private bool initialized;
        private bool paused;
        private float transitionEnd;
        // 代理会被复用，序号和资源都仍有效才属于这一次播放。
        private bool HasAgent => agent != null && agent.SerialId == serialId && agent.Length > 0;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                // 等 Sound 与 Event 完成初始化后再建组和订阅事件。
                await UniTask.NextFrame(cancellation);
                // 一个代理同一时间承载一路声音；播放完成后可以复用。
                QGF.Sound.AddSoundGroup("Music", 1);
                // PlaySound 先返回序号，成功事件才提供实际播放的代理。
                QGF.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
                fadeDropdown.ClearOptions();
                fadeDropdown.AddOptions(new List<string> { "默认（立即）", "淡入淡出 0.6 秒" });
                controlDropdown.ClearOptions();
                controlDropdown.AddOptions(new List<string> { "按播放序号操作", "直接操作当前代理" });
                playButton.onClick.AddListener(Play);
                pauseButton.onClick.AddListener(Pause);
                resumeButton.onClick.AddListener(Resume);
                stopButton.onClick.AddListener(() =>
                {
                    // 停止结束本次播放；带时长的重载会先淡出再释放资源。
                    if (controlDropdown.value == 0)
                    {
                        if (fadeDropdown.value == 0) QGF.Sound.StopSound(serialId);
                        else QGF.Sound.StopSound(serialId, 0.6f);
                    }
                    else if (HasAgent)
                    {
                        if (fadeDropdown.value == 0) agent.Stop();
                        else agent.Stop(0.6f);
                    }
                    paused = false;
                });
                replayButton.onClick.AddListener(() =>
                {
                    // 代理已经持有音频，Play 从头重播，不发起资源加载或申请新序号。
                    if (!HasAgent) return;
                    // 暂停可能把输出降为 0；重播先恢复本例的基础音量。
                    agent.VolumeInSoundGroup = 0.3f;
                    if (fadeDropdown.value == 0) agent.Play();
                    else agent.Play(0.6f);
                    paused = false;
                    transitionEnd = Time.unscaledTime + (fadeDropdown.value == 0 ? 0 : 0.6f);
                });
                resetButton.onClick.AddListener(() =>
                {
                    // Reset 归还音频资源并恢复代理参数；下次播放仍需通过组件加载。
                    if (!HasAgent) return;
                    agent.Reset();
                    paused = false;
                });
                loopToggle.onValueChanged.AddListener(value => { if (HasAgent) agent.Loop = value; });
                pitchSlider.onValueChanged.AddListener(value => { if (HasAgent) agent.Pitch = value; });
                seekSlider.onValueChanged.AddListener(value =>
                {
                    // Time 用秒；滑块用归一化位置，避开片尾边界。
                    if (HasAgent) agent.Time = value * (agent.Length - 0.01f);
                });
                initialized = true;
                Play();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Play()
        {
            // PlaySound 接管播放参数，处理后归还引用池。
            var parameters = PlaySoundParams.Create();
            parameters.Loop = loopToggle.isOn;
            parameters.Pitch = pitchSlider.value;
            parameters.VolumeInSoundGroup = 0.3f;
            parameters.FadeInSeconds = fadeDropdown.value == 0 ? 0 : 0.6f;
            // 参数作用于当前播放代理。
            serialId = QGF.Sound.PlaySound(UnityEditor.AssetDatabase.GUIDToAssetPath(soundGuid), "Music", parameters);
            agent = null;
            paused = false;
        }

        private void Pause()
        {
            // 暂停保留代理与位置，恢复时不重新加载声音。
            if (controlDropdown.value == 0)
            {
                if (fadeDropdown.value == 0) QGF.Sound.PauseSound(serialId);
                else QGF.Sound.PauseSound(serialId, 0.6f);
            }
            else if (HasAgent)
            {
                // 缓存代理可能被复用，先用 HasAgent 确认它仍承载本次序号。
                if (fadeDropdown.value == 0) agent.Pause();
                else agent.Pause(0.6f);
            }
            paused = true;
            transitionEnd = Time.unscaledTime + (fadeDropdown.value == 0 ? 0 : 0.6f);
        }

        private void Resume()
        {
            // 恢复暂停的位置与音量，带时长时渐入到暂停前的音量。
            if (controlDropdown.value == 0)
            {
                if (fadeDropdown.value == 0) QGF.Sound.ResumeSound(serialId);
                else QGF.Sound.ResumeSound(serialId, 0.6f);
            }
            else if (HasAgent)
            {
                if (fadeDropdown.value == 0) agent.Resume();
                else agent.Resume(0.6f);
            }
            paused = false;
            transitionEnd = Time.unscaledTime + (fadeDropdown.value == 0 ? 0 : 0.6f);
        }

        private void OnPlayed(object sender, GameEventArgs args)
        {
            var e = (PlaySoundSuccessEventArgs)args;
            if (e.SerialId == serialId) agent = e.SoundAgent;
        }

        private void Update()
        {
            if (!initialized) return;
            bool active = HasAgent;
            // 淡入淡出期间锁住相反操作，避免用户连续点击覆盖同一段渐变。
            bool stable = Time.unscaledTime >= transitionEnd;
            seekSlider.interactable = active && stable;
            seekSlider.SetValueWithoutNotify(active ? agent.Time / agent.Length : 0);
            playButton.interactable = !active && !QGF.Sound.IsLoadingSound(serialId);
            pauseButton.interactable = active && stable && !paused;
            resumeButton.interactable = active && stable && paused;
            stopButton.interactable = active;
            replayButton.interactable = resetButton.interactable = active && stable && controlDropdown.value == 1;
            outputText.text = active
                ? $"序号：{serialId}    {(paused ? "暂停" : "播放")}\n位置：{agent.Time:F1} / {agent.Length:F1} 秒"
                    + $"    音调：{agent.Pitch:F2}\n循环：{(agent.Loop ? "开启" : "关闭")}    当前输出音量：{agent.Volume:F2}"
                : QGF.Sound.IsLoadingSound(serialId) ? "正在加载" : "已停止";
        }

        private void OnDestroy()
        {
            // 只给仍存在的组件解绑，框架可能已经退出。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(PlaySoundSuccessEventArgs.EventId, OnPlayed))
                events.Unsubscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
            var sound = GameEntry.GetComponent<SoundComponent>();
            if (sound != null && serialId != 0) sound.StopSound(serialId);
        }
    }
}
#endif
