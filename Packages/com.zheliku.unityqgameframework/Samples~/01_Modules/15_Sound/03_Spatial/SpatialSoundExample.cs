using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示三维声音的位置、距离和声相。
    /// </summary>
    public sealed class SpatialSoundExample : MonoBehaviour
    {
        private const string AssetName = "Sound_Spatial_Tone";
        [SerializeField]
        private Transform listenerMarker, sourceMarker;
        [SerializeField]
        private Slider positionSlider, distanceSlider, blendSlider, panSlider;
        [SerializeField]
        private Button playButton, stopButton;
        [SerializeField]
        private Text stateText;
        private ISoundAgent agent;
        private int serialId;
        private bool ready;
        private bool HasAgent
        {
            get
            {
                return agent != null && agent.SerialId == serialId && agent.Length > 0;
            }
        }

        private void Start()
        {
            // 资源模式和事件组件先初始化，之后才能提交播放请求。
            // 创建一路声音，组音量设为 0.3。
            QGF.Sound.AddSoundGroup("World", false, false, 0.3f, 1);
            // 成功后才能取得代理并修改空间参数。
            QGF.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
            playButton.onClick.AddListener(Play);
            stopButton.onClick.AddListener(() => QGF.Sound.StopSound(serialId));
            // 固定坐标只在 PlaySound 时设置；移动标记后重新播放，才能改变声源位置。
            positionSlider.onValueChanged.AddListener(value => sourceMarker.position = new Vector3(4 + value, 0, Mathf.Abs(value) * 0.6f));
            distanceSlider.onValueChanged.AddListener(value =>
            {
                if (HasAgent)
                {
                    agent.MaxDistance = value;
                }
            });
            blendSlider.onValueChanged.AddListener(value =>
            {
                if (HasAgent)
                {
                    agent.SpatialBlend = value;
                }
            });
            panSlider.onValueChanged.AddListener(value =>
            {
                if (HasAgent)
                {
                    agent.PanStereo = value;
                }
            });
            sourceMarker.position = new Vector3(4 + positionSlider.value, 0, Mathf.Abs(positionSlider.value) * 0.6f);
            ready = true;
            Play();
        }

        private void Play()
        {
            // 应用新坐标前结束旧序号，避免把一次试听叠成多路声音。
            if (serialId != 0)
            {
                QGF.Sound.StopSound(serialId);
            }

            Vector3 position = sourceMarker.position;
            // 把当前位置传给 Sound；成功后在代理上设置 3D 播放属性。
            serialId = QGF.Sound.PlaySound(AssetName, "World", position);
            agent = null;
        }

        private void OnPlayed(object sender, GameEventArgs args)
        {
            var e = (PlaySoundSuccessEventArgs)args;
            if (e.SerialId != serialId)
            {
                return;
            }

            // 只记录这次请求的代理；旧请求的事件在上面已排除。
            agent = e.SoundAgent;
            agent.Loop = true;
            agent.SpatialBlend = blendSlider.value;
            agent.MaxDistance = distanceSlider.value;
            agent.PanStereo = panSlider.value;
            // 衰减曲线由 Unity 的 AudioSource 管理；这里用线性曲线对比距离变化。
            var source = ((SoundAgentHelperBase)agent.Helper).GetComponent<AudioSource>();
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1;
        }

        private void Update()
        {
            if (!ready)
            {
                return;
            }

            bool active = HasAgent;
            // Helper 是 Unity 播放对象；Volume 不包含音频系统计算的距离衰减。
            var source = active ? (SoundAgentHelperBase)agent.Helper : null;
            float distance = active ? Vector3.Distance(source.transform.position, listenerMarker.position) : 0;
            stateText.text = active ? $"Serial: {serialId}    Distance: {distance:F1}/{agent.MaxDistance:F1} m\n3D blend: {agent.SpatialBlend:F2}    Stereo pan: {agent.PanStereo:F2}\n" + (Vector3.Distance(source.transform.position, sourceMarker.position) > .01f ? "Position changed. Click Play to apply." : "Playing at a fixed position.") : QGF.Sound.IsLoadingSound(serialId) ? "Loading" : "Stopped";
            playButton.interactable = !QGF.Sound.IsLoadingSound(serialId);
            stopButton.interactable = active;
        }

        private void OnDestroy()
        {
            // 只给仍存在的组件解绑，框架可能已经退出。
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
