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
    public sealed class SoundGroupVolumeExample : MonoBehaviour
    {
        [SerializeField] private string soundGuid;
        [SerializeField] private Slider groupVolumeSlider;
        [SerializeField] private Slider voiceVolumeSlider;
        [SerializeField] private Toggle groupMuteToggle;
        [SerializeField] private Toggle voiceMuteToggle;
        [SerializeField] private Dropdown stopModeDropdown;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Text outputText;

        private readonly List<ISoundGroup> groups = new List<ISoundGroup>();
        private ISoundGroup music;
        private ISoundAgent agent;
        private int serialId;
        private int effectsSerialId;
        private ISoundAgent effectsAgent;
        private bool initialized;
        private bool HasAgent => agent != null && agent.SerialId == serialId && agent.Length > 0;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // 一般在启动时创建一次；也可以在 SoundComponent Inspector 配置声音组。
                if (!QGF.Sound.HasSoundGroup("Music")) QGF.Sound.AddSoundGroup("Music", 1);
                QGF.Sound.AddSoundGroup("Effects", 1);
                // 取得的是运行中的组；修改它会立即作用于组内所有声音。
                music = QGF.Sound.GetSoundGroup("Music");
                music.Volume = groupVolumeSlider.value;
                // 成功回调按序号分别记录 Music 与 Effects 的代理。
                QGF.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
                // 最终音量 = 组音量 × 本声音量；两个静音标记任意为真就静音。
                groupVolumeSlider.onValueChanged.AddListener(value => music.Volume = value);
                voiceVolumeSlider.onValueChanged.AddListener(value => { if (HasAgent) agent.VolumeInSoundGroup = value; });
                groupMuteToggle.onValueChanged.AddListener(value => music.Mute = value);
                voiceMuteToggle.onValueChanged.AddListener(value => { if (HasAgent) agent.MuteInSoundGroup = value; });
                stopModeDropdown.ClearOptions();
                stopModeDropdown.AddOptions(new List<string>
                {
                    "停止 Music 组", "淡出 Music 组", "停止全部声音", "淡出全部声音"
                });
                stopButton.onClick.AddListener(() =>
                {
                    // 组接口只停止 Music；组件接口停止所有声音组，包含 Effects。
                    switch (stopModeDropdown.value)
                    {
                        case 0: music.StopAllLoadedSounds(); break;
                        case 1: music.StopAllLoadedSounds(0.6f); break;
                        case 2: QGF.Sound.StopAllLoadedSounds(); break;
                        default: QGF.Sound.StopAllLoadedSounds(0.6f); break;
                    }
                });
                playButton.onClick.AddListener(Play);
                initialized = true;
                Play();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Play()
        {
            // 每次播放重新申请参数，框架处理请求后负责归还。
            var parameters = PlaySoundParams.Create();
            parameters.Loop = true;
            parameters.VolumeInSoundGroup = voiceVolumeSlider.value;
            parameters.MuteInSoundGroup = voiceMuteToggle.isOn;
            serialId = QGF.Sound.PlaySound(UnityEditor.AssetDatabase.GUIDToAssetPath(soundGuid), "Music", parameters);
            agent = null;
            // 只停 Music 后 Effects 仍在播放，重开时不要再叠加一份。
            if (effectsAgent == null || effectsAgent.Length == 0)
            {
                var effect = PlaySoundParams.Create();
                effect.Loop = true;
                effect.Pitch = 1.5f;
                effect.VolumeInSoundGroup = 0.08f;
                effectsSerialId = QGF.Sound.PlaySound(UnityEditor.AssetDatabase.GUIDToAssetPath(soundGuid), "Effects", effect);
            }
        }

        private void OnPlayed(object sender, GameEventArgs args)
        {
            var e = (PlaySoundSuccessEventArgs)args;
            if (e.SerialId == serialId) agent = e.SoundAgent;
            if (e.SerialId == effectsSerialId) effectsAgent = e.SoundAgent;
        }

        private void Update()
        {
            if (!initialized) return;
            // 两个查询重载：数组方便直接使用，List 适合反复查询。
            var array = QGF.Sound.GetAllSoundGroups();
            QGF.Sound.GetAllSoundGroups(groups);
            bool active = HasAgent;
            // 组辅助器连接 Mixer 分组；播放代理的 SoundGroup 是实际所属组。
            var mixer = QGF.Sound.AudioMixer;
            string muteState;
            if (active)
            {
                string finalState = agent.Mute ? "静音" : "有声";
                string groupState = music.Mute ? "静音" : "有声";
                string voiceState = agent.MuteInSoundGroup ? "静音" : "有声";
                muteState = $"最终状态：{finalState}（组：{groupState}；本声：{voiceState}）";
            }
            else muteState = $"{music.Name} 已停止";
            outputText.text = $"声音组：{QGF.Sound.SoundGroupCount}（数组 {array.Length} / List {groups.Count}）"
                + $"\n{music.Name} 代理数：{music.SoundAgentCount}    "
                + $"Effects 组：{(QGF.Sound.HasSoundGroup("Effects") ? "已注册" : "未注册")}"
                + $"\n组辅助器：{music.Helper.GetType().Name}；Mixer：{(mixer != null ? mixer.name : "未分配")}"
                + $"\n组音量 {music.Volume:F2} × 本声音量 {voiceVolumeSlider.value:F2}"
                + (active ? $" = {agent.Volume:F2}\n" : "\n")
                + muteState
                + (active ? $"\n当前代理所属组：{agent.SoundGroup.Name}" : string.Empty)
                + $"\nEffects：{(effectsAgent != null && effectsAgent.IsPlaying ? "播放" : "停止")}";
            playButton.interactable = !active && !QGF.Sound.IsLoadingSound(serialId);
            stopButton.interactable = active;
        }

        private void OnDestroy()
        {
            // 只给仍存在的组件解绑，框架可能已经退出。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(PlaySoundSuccessEventArgs.EventId, OnPlayed))
                events.Unsubscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
            var sound = GameEntry.GetComponent<SoundComponent>();
            if (sound != null)
            {
                if (serialId != 0) sound.StopSound(serialId);
                if (effectsSerialId != 0) sound.StopSound(effectsSerialId);
            }
        }
    }
}
#endif
