using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Sound;
using UnityQGameFramework.Runtime;
using PlaySoundSuccessEventArgs = UnityQGameFramework.Runtime.PlaySoundSuccessEventArgs;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示声音组音量与单次播放音量。
    /// </summary>
    public sealed class SoundGroupVolumeExample : MonoBehaviour
    {
        private const string AssetName = "Sound_GroupVolume_Tone";
        [SerializeField]
        private Slider groupVolumeSlider, voiceVolumeSlider;
        [SerializeField]
        private Toggle groupMuteToggle, voiceMuteToggle;
        [SerializeField]
        private Button playButton, stopButton;
        [SerializeField]
        private Text outputText;
        private ISoundGroup music;
        private ISoundAgent agent;
        private int serialId;
        private bool HasAgent
        {
            get
            {
                return agent != null && agent.SerialId == serialId && agent.Length > 0;
            }
        }

        private void Start()
        {
            if (!QGF.Sound.HasSoundGroup("Music"))
            {
                QGF.Sound.AddSoundGroup("Music", 1);
            }

            music = QGF.Sound.GetSoundGroup("Music");
            music.Volume = groupVolumeSlider.value;
            QGF.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
            groupVolumeSlider.onValueChanged.AddListener(value => music.Volume = value);
            groupMuteToggle.onValueChanged.AddListener(value => music.Mute = value);
            voiceVolumeSlider.onValueChanged.AddListener(value =>
            {
                if (HasAgent)
                {
                    agent.VolumeInSoundGroup = value;
                }
            });
            voiceMuteToggle.onValueChanged.AddListener(value =>
            {
                if (HasAgent)
                {
                    agent.MuteInSoundGroup = value;
                }
            });
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
            serialId = QGF.Sound.PlaySound(AssetName, "Music", parameters);
            agent = null;
        }

        private void OnPlayed(object sender, GameEventArgs args)
        {
            var result = (PlaySoundSuccessEventArgs)args;
            if (result.SerialId == serialId)
            {
                agent = result.SoundAgent;
            }
        }

        private void Update()
        {
            string state = !HasAgent ? "No sound is playing in the music group." : $"Final volume: {agent.Volume:F2} = Group {music.Volume:F2} × Voice {voiceVolumeSlider.value:F2}\n" + $"Group muted: {(music.Mute ? "Yes" : "No")}    Voice muted: {(agent.MuteInSoundGroup ? "Yes" : "No")}\n" + $"Final state: {(agent.Mute ? "Muted" : "Audible")}";
            outputText.text = state;
            playButton.interactable = !HasAgent && !QGF.Sound.IsLoadingSound(serialId);
            stopButton.interactable = HasAgent;
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(PlaySoundSuccessEventArgs.EventId, OnPlayed))
            {
                events.Unsubscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
            }

            var sound = GameEntry.GetComponent<SoundComponent>();
            if (sound != null && serialId != 0)
            {
                sound.StopSound(serialId);
            }
        }
    }
}
