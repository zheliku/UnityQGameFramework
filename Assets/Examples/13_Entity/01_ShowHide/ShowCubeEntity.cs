#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ShowCubeEntity : MonoBehaviour
    {
        [SerializeField] private string prefabGuid;
        [SerializeField] private Dropdown showMode, hideMode;
        [SerializeField] private Button showButton, hideButton, cancelButton;
        [SerializeField] private Text stateText, lifecycleText;
        private string asset, failure;
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                asset = UnityEditor.AssetDatabase.GUIDToAssetPath(prefabGuid);
                // 实体组管理实例缓存，容量用于空闲回收。
                if (!QGF.Entity.HasEntityGroup("Example"))
                    QGF.Entity.AddEntityGroup("Example", 30f, 8, 60f, 0);
                // QGF.Event 持有订阅；销毁时解除，避免保留当前组件。
                QGF.Event.Subscribe(ShowEntityFailureEventArgs.EventId, OnFailed);
                showMode.ClearOptions();
                showMode.AddOptions(new List<string> { "泛型", "Type", "泛型 + 优先级", "Type + 优先级",
                    "泛型 + 数据", "Type + 数据", "泛型 + 优先级和数据", "Type + 优先级和数据" });
                hideMode.ClearOptions();
                hideMode.AddOptions(new List<string> { "按编号", "按编号 + 数据", "按实体", "按实体 + 数据" });
                showButton.onClick.AddListener(Show);
                hideButton.onClick.AddListener(() =>
                {
                    // GetEntity 取得已显示的实体，未显示时返回 null。
                    var entity = QGF.Entity.GetEntity(1);
                    switch (hideMode.value)
                    {
                        // HideEntity 结束显示，实例由框架回池。
                        case 0: QGF.Entity.HideEntity(1); break;
                        case 1: QGF.Entity.HideEntity(1, this); break;
                        case 2: QGF.Entity.HideEntity(entity); break;
                        case 3: QGF.Entity.HideEntity(entity, this); break;
                    }
                });
                // 取消的是待显示的请求；资源读完后也不会把这批实体显示出来。
                cancelButton.onClick.AddListener(() => QGF.Entity.HideAllLoadingEntities());
                ready = true;
                Show();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Show()
        {
            failure = null;
            var position = new Vector3(3, 0, 0); // 通过 userData 传入 OnShow。
            switch (showMode.value)
            {
                // 显示可能先异步加载资源；成功后才能按编号取得实体，数据传给 OnShow。
                case 0: QGF.Entity.ShowEntity<ShowCubeEntityLogic>(1, asset, "Example"); break;
                case 1: QGF.Entity.ShowEntity(1, typeof(ShowCubeEntityLogic), asset, "Example"); break;
                case 2: QGF.Entity.ShowEntity<ShowCubeEntityLogic>(1, asset, "Example", 10); break;
                case 3: QGF.Entity.ShowEntity(1, typeof(ShowCubeEntityLogic), asset, "Example", 10); break;
                case 4: QGF.Entity.ShowEntity<ShowCubeEntityLogic>(1, asset, "Example", position); break;
                case 5: QGF.Entity.ShowEntity(1, typeof(ShowCubeEntityLogic), asset, "Example", position); break;
                case 6: QGF.Entity.ShowEntity<ShowCubeEntityLogic>(1, asset, "Example", 10, position); break;
                case 7: QGF.Entity.ShowEntity(1, typeof(ShowCubeEntityLogic), asset, "Example", 10, position); break;
            }
        }

        private void Update()
        {
            if (!ready) return;
            // 加载中的编号还没有可用的 Entity 实例。
            bool loading = QGF.Entity.IsLoadingEntity(1);
            var entity = QGF.Entity.GetEntity(1);
            stateText.text = failure ?? (loading ? "实体 1 正在加载，可以取消" : entity == null ? "实体 1 未显示"
                : $"实体 {entity.Id} 已显示；状态：{(QGF.Entity.IsValidEntity(entity) ? "有效" : "无效")}");
            var logic = entity == null ? null : (ShowCubeEntityLogic)entity.Logic;
            // 查询当前已加载或待加载集合；数组和 List 入口都只反映调用时的状态。
            lifecycleText.text = logic == null ? $"待加载：{QGF.Entity.GetAllLoadingEntityIds().Length}    已显示：{QGF.Entity.EntityCount}"
                : $"实例：{entity.gameObject.GetInstanceID()}\nOnInit：{logic.InitCalls}    OnShow：{logic.ShowCalls}\n"
                    + $"本次 OnShow：{(logic.HasShowData ? "已收到数据" : "未传数据")}";
            showButton.interactable = !loading && entity == null;
            hideButton.interactable = entity != null;
            cancelButton.interactable = loading;
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var e = (ShowEntityFailureEventArgs)args;
            if (e.EntityId == 1) failure = "显示失败：" + e.ErrorMessage;
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(ShowEntityFailureEventArgs.EventId, OnFailed))
                events.Unsubscribe(ShowEntityFailureEventArgs.EventId, OnFailed);
            // 控制脚本退出时只隐藏自己的编号，加载中的请求也用 HideEntity 取消。
            var entities = GameEntry.GetComponent<EntityComponent>();
            if (ready && entities != null && (entities.HasEntity(1) || entities.IsLoadingEntity(1))) entities.HideEntity(1);
        }
    }
}
#endif
