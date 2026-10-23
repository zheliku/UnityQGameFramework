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
    public sealed class FollowEntitySound : MonoBehaviour
    {
        [SerializeField] private string soundGuid, prefabGuid;
        [SerializeField] private Transform listenerMarker;
        [SerializeField] private Dropdown playMode;
        [SerializeField] private Slider dopplerSlider;
        [SerializeField] private Button playButton, hideButton, showButton;
        [SerializeField] private Text stateText;

        private ISoundAgent agent;
        private int serialId;
        private bool ready;
        private bool HasAgent => agent != null && agent.SerialId == serialId && agent.Length > 0;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                // 等 Entity、Sound 和 Event 完成初始化后再使用它们。
                await UniTask.NextFrame(cancellation);
                // 实体实例缓存容量为 4，空闲 60 秒后可回收。
                QGF.Entity.AddEntityGroup("Emitters", 30f, 4, 60f, 0);
                // 建一路空间声音，组音量统一设为 0.3。
                QGF.Sound.AddSoundGroup("World", false, false, 0.3f, 1);
                // 成功事件提供加载完成后的声音代理。
                QGF.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
                playMode.ClearOptions();
                playMode.AddOptions(new List<string> { "实体", "加载优先级 + 参数 + 实体", "完整参数 + 实体 + 上下文" });
                playButton.onClick.AddListener(Play);
                // 隐藏实体后，默认声音辅助器发现 Logic 不再 Available，会自动重置这路声音。
                hideButton.onClick.AddListener(() => QGF.Entity.HideEntity(1));
                showButton.onClick.AddListener(Show);
                dopplerSlider.onValueChanged.AddListener(value => { if (HasAgent) agent.DopplerLevel = value; });
                ready = true;
                Show();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Show()
        {
            // 先显示实体，加载完成后才能将 Entity 传给声音模块。
            QGF.Entity.ShowEntity<SoundEmitterLogic>(1, UnityEditor.AssetDatabase.GUIDToAssetPath(prefabGuid), "Emitters");
        }

        private void Play()
        {
            // 重新绑定前停止旧播放；实体保持显示，不必重新加载。
            if (serialId != 0) QGF.Sound.StopSound(serialId);
            string asset = UnityEditor.AssetDatabase.GUIDToAssetPath(soundGuid);
            // 按钮只在实体已加载时可点，这里取得可绑定的 Unity Entity。
            var entity = QGF.Entity.GetEntity(1);
            // 三个重载都绑定实体，辅助器负责跟随；this 会原样带回成功事件。
            switch (playMode.value)
            {
                case 0: serialId = QGF.Sound.PlaySound(asset, "World", entity); break;
                case 1: serialId = QGF.Sound.PlaySound(asset, "World", 0, PlaySoundParams.Create(), entity); break;
                default: serialId = QGF.Sound.PlaySound(asset, "World", 0, PlaySoundParams.Create(), entity, this); break;
            }
            agent = null;
        }

        private void OnPlayed(object sender, GameEventArgs args)
        {
            var e = (PlaySoundSuccessEventArgs)args;
            if (e.SerialId != serialId) return;
            // 空间参数作用于当前声音代理。
            agent = e.SoundAgent;
            agent.Loop = true;
            agent.SpatialBlend = 1;
            agent.MaxDistance = 10;
            agent.DopplerLevel = dopplerSlider.value;
            // 衰减曲线由 Unity 管理，这里采用线性曲线，便于比较远近。
            var source = ((SoundAgentHelperBase)agent.Helper).GetComponent<AudioSource>();
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1;
        }

        private void LateUpdate()
        {
            if (!ready) return;
            var entity = QGF.Entity.GetEntity(1);
            bool active = HasAgent;
            float distance = entity != null ? Vector3.Distance(entity.transform.position, listenerMarker.position) : 0;
            stateText.text = entity == null ? "实体未显示；绑定声音已停止。"
                : $"实体：{entity.Id}    距离：{distance:F1} 米\n"
                    + $"声音：{(active ? "跟随播放" : "未播放")}    序号：{serialId}\n"
                    + $"多普勒：{(active ? agent.DopplerLevel : dopplerSlider.value):F1}";
            playButton.interactable = entity != null && !QGF.Sound.IsLoadingSound(serialId);
            hideButton.interactable = entity != null;
            showButton.interactable = entity == null && !QGF.Entity.IsLoadingEntity(1);
        }

        private void OnDestroy()
        {
            // 只给仍存在的组件解绑，框架可能已经退出。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(PlaySoundSuccessEventArgs.EventId, OnPlayed))
                events.Unsubscribe(PlaySoundSuccessEventArgs.EventId, OnPlayed);
            var sound = GameEntry.GetComponent<SoundComponent>();
            if (sound != null && serialId != 0) sound.StopSound(serialId);
            // 发声实体由本示例创建，退出时一并隐藏或取消待显示请求。
            var entities = GameEntry.GetComponent<EntityComponent>();
            if (ready && entities != null && (entities.HasEntity(1) || entities.IsLoadingEntity(1))) entities.HideEntity(1);
        }
    }
}
#endif
