#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Sound;
using UnityQGameFramework.Runtime;
using PlaySoundSuccessEventArgs = UnityQGameFramework.Runtime.PlaySoundSuccessEventArgs;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class SoundGroupVolumeExample : MonoBehaviour
    {
        [SerializeField] private string soundGuid;
        [SerializeField] private Slider groupVolumeSlider, voiceVolumeSlider;
        [SerializeField] private Toggle groupMuteToggle, voiceMuteToggle;
        [SerializeField] private Button playButton, stopButton;
        [SerializeField] private Text outputText;

        private ISoundGroup music;
        private ISoundAgent agent;
        private int serialId;

        private bool HasAgent => agent != null && agent.SerialId == serialId && agent.Length > 0;

        private void Start()
        {
            if (!QGF.Sound.HasSoundGroup("Music")) QGF.Sound.AddSoundGroup("Music", 1);
            music = QGF.Sound.GetSoundGroup("Music");
            music.Volume = groupVolumeSlider.value;

            QGF.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
            groupVolumeSlider.onValueChanged.AddListener(value => music.Volume = value);
            groupMuteToggle.onValueChanged.AddListener(value => music.Mute = value);
            voiceVolumeSlider.onValueChanged.AddListener(value => { if (HasAgent) agent.VolumeInSoundGroup = value; });
            voiceMuteToggle.onValueChanged.AddListener(value => { if (HasAgent) agent.MuteInSoundGroup = value; });
            playButton.onClick.AddListener(Play);
            stopButton.onClick.AddListener(() => music.StopAllLoadedSounds());
            Play();
        }

        private void Play()
        {
            var parameters = PlaySoundParams.Create();
            parameters.Loop = true;
            parameters.VolumeInSoundGroup = voiceVolumeSlider.value;
            parameters.MuteInSoundGroup = voiceMuteToggle.isOn;
            serialId = QGF.Sound.PlaySound(
                UnityEditor.AssetDatabase.GUIDToAssetPath(soundGuid), "Music", parameters);
            agent = null;
        }

        private void OnPlayed(object sender, GameEventArgs args)
        {
            var result = (PlaySoundSuccessEventArgs)args;
            if (result.SerialId == serialId) agent = result.SoundAgent;
        }

        private void Update()
        {
            string state = !HasAgent ? "音乐组当前没有播放声音。"
                : $"最终音量：{agent.Volume:F2} = 组 {music.Volume:F2} × 单声道 {voiceVolumeSlider.value:F2}\n"
                + $"组静音：{(music.Mute ? "是" : "否")}    单声道静音：{(agent.MuteInSoundGroup ? "是" : "否")}\n"
                + $"最终状态：{(agent.Mute ? "静音" : "有声")}";
            outputText.text = state;
            playButton.interactable = !HasAgent && !QGF.Sound.IsLoadingSound(serialId);
            stopButton.interactable = HasAgent;
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(PlaySoundSuccessEventArgs.EventId, OnPlayed))
                events.Unsubscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);

            var sound = GameEntry.GetComponent<SoundComponent>();
            if (sound != null && serialId != 0) sound.StopSound(serialId);
        }
    }
}
#endif
