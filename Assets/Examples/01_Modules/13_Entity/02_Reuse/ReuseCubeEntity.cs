#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class ReuseCubeEntity : MonoBehaviour
    {
        [SerializeField] private string prefabGuid;
        [SerializeField] private Slider idleSlider;
        [SerializeField] private Toggle lockedToggle;
        [SerializeField] private Button recycleButton;
        [SerializeField] private Text stateText, policyText, lifecycleText;
        private string asset;
        private int previousInstance, currentInstance;
        private float idleRemaining;
        private bool ready, recycling;

        private void Start()
        {
            asset = UnityEditor.AssetDatabase.GUIDToAssetPath(prefabGuid);
            // 实体组管理实例缓存，容量用于空闲回收。
            QGF.Entity.AddEntityGroup("Example", 0.2f, 8, 1f, 0);
            QGF.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShown);
            recycleButton.onClick.AddListener(HideAndShow);
            ready = true;
            // 显示可能先异步加载资源；成功后才能按编号取得实体，数据传给 OnShow。
            QGF.Entity.ShowEntity<ReuseCubeEntityLogic>(1, asset, "Example", new Vector3(3, 0, 0));
        }

        private async void HideAndShow()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                recycling = true;
                // GetEntity 取得已显示的实体，未显示时返回 null。
                var entity = QGF.Entity.GetEntity(1);
                previousInstance = entity.gameObject.GetInstanceID(); // 归还后只保留编号，不再使用这份引用。
                QGF.Entity.SetEntityInstanceLocked(entity, lockedToggle.isOn);
                QGF.Entity.SetInstancePriority(entity, 10);
                // HideEntity 结束显示，实例由框架回池。
                QGF.Entity.HideEntity(entity);
                // 等下一帧的帧末，完成回池后再显示。
                await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate, cancellation);
                float end = Time.realtimeSinceStartup + idleSlider.value;
                while (Time.realtimeSinceStartup < end)
                {
                    idleRemaining = end - Time.realtimeSinceStartup;
                    await UniTask.NextFrame(cancellation);
                }
                idleRemaining = 0;
                QGF.Entity.ShowEntity<ReuseCubeEntityLogic>(1, asset, "Example", new Vector3(3, 0, 0));
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
            finally { recycling = false; }
        }

        private void OnShown(object sender, GameEventArgs args)
        {
            var e = (ShowEntitySuccessEventArgs)args;
            if (e.Entity.Id == 1) currentInstance = ((Entity)e.Entity).gameObject.GetInstanceID();
        }

        private void Update()
        {
            if (!ready) return;
            var entity = QGF.Entity.GetEntity(1);
            var group = QGF.Entity.GetEntityGroup("Example");
            policyText.text = $"空闲过期：{group.InstanceExpireTime:F1} 秒    本次等待：{idleSlider.value:F1} 秒\n锁定的实例不会因空闲过期被释放";
            stateText.text = recycling ? $"已隐藏，等待 {idleRemaining:F1} 秒后再显示" : entity == null ? "正在加载"
                : $"当前实例：{currentInstance}\n上次实例：{previousInstance}\n"
                    + $"本次来源：{(previousInstance != 0 && currentInstance == previousInstance ? "复用" : "新建")}";
            var logic = entity == null ? null : (ReuseCubeEntityLogic)entity.Logic;
            lifecycleText.text = logic == null
                ? "实例未显示"
                : $"本实例 OnInit：{logic.InitCalls}    OnShow：{logic.ShowCalls}\n"
                    + $"OnHide：{logic.HideCalls}    OnRecycle：{logic.RecycleCalls}";
            recycleButton.interactable = entity != null && !recycling;
            idleSlider.interactable = lockedToggle.interactable = !recycling;
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(ShowEntitySuccessEventArgs.EventId, OnShown))
                events.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShown);
            // UniTask 随脚本销毁而取消；已显示或仍在加载的实体也要归还。
            var entities = GameEntry.GetComponent<EntityComponent>();
            if (ready && entities != null && (entities.HasEntity(1) || entities.IsLoadingEntity(1))) entities.HideEntity(1);
        }
    }
}
#endif
