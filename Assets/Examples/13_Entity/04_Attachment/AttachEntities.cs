#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Entity;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class AttachEntities : MonoBehaviour
    {
        [SerializeField] private string prefabGuid;
        [SerializeField] private Dropdown attachMode, detachMode;
        [SerializeField] private Button attachButton, detachButton, hideParentButton, showButton, queryButton;
        [SerializeField] private Text relationText, callbackText, queryText;
        private string asset;
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                asset = UnityEditor.AssetDatabase.GUIDToAssetPath(prefabGuid);
                // 实体组管理实例缓存，容量用于空闲回收。
                QGF.Entity.AddEntityGroup("Example", 30f, 8, 60f, 0);
                attachMode.ClearOptions();
                var choices = new List<string>();
                foreach (string pair in new[] { "编号 → 编号", "编号 → 实例", "实例 → 编号", "实例 → 实例" })
                    foreach (string target in new[] { "根节点", "根节点 + 数据", "路径", "路径 + 数据", "Transform", "Transform + 数据" })
                        choices.Add(pair + " / " + target);
                attachMode.AddOptions(choices);
                detachMode.ClearOptions();
                detachMode.AddOptions(new List<string> { "子编号", "子编号 + 数据", "子实例", "子实例 + 数据", "父编号下全部", "父编号下全部 + 数据", "父实例下全部", "父实例下全部 + 数据" });
                attachButton.onClick.AddListener(() =>
                {
                    // GetEntity 取得已显示的实体，未显示时返回 null。
                    var parent = QGF.Entity.GetEntity(10);
                    var child = QGF.Entity.GetEntity(11);
                    var socket = ((AttachedCubeLogic)parent.Logic).Socket;
                    // 四组分别使用编号或实例定位父子；组内比较根、路径、Transform 及上下文。
                    // 根节点的上下文要转成 object，直接传 string 会选中路径重载。
                    switch (attachMode.value)
                    {
                        case 0: QGF.Entity.AttachEntity(11, 10); break;
                        case 1: QGF.Entity.AttachEntity(11, 10, (object)"挂接数据"); break;
                        case 2: QGF.Entity.AttachEntity(11, 10, "Socket"); break;
                        case 3: QGF.Entity.AttachEntity(11, 10, "Socket", "挂接数据"); break;
                        case 4: QGF.Entity.AttachEntity(11, 10, socket); break;
                        case 5: QGF.Entity.AttachEntity(11, 10, socket, "挂接数据"); break;
                        case 6: QGF.Entity.AttachEntity(11, parent); break;
                        case 7: QGF.Entity.AttachEntity(11, parent, (object)"挂接数据"); break;
                        case 8: QGF.Entity.AttachEntity(11, parent, "Socket"); break;
                        case 9: QGF.Entity.AttachEntity(11, parent, "Socket", "挂接数据"); break;
                        case 10: QGF.Entity.AttachEntity(11, parent, socket); break;
                        case 11: QGF.Entity.AttachEntity(11, parent, socket, "挂接数据"); break;
                        case 12: QGF.Entity.AttachEntity(child, 10); break;
                        case 13: QGF.Entity.AttachEntity(child, 10, (object)"挂接数据"); break;
                        case 14: QGF.Entity.AttachEntity(child, 10, "Socket"); break;
                        case 15: QGF.Entity.AttachEntity(child, 10, "Socket", "挂接数据"); break;
                        case 16: QGF.Entity.AttachEntity(child, 10, socket); break;
                        case 17: QGF.Entity.AttachEntity(child, 10, socket, "挂接数据"); break;
                        case 18: QGF.Entity.AttachEntity(child, parent); break;
                        case 19: QGF.Entity.AttachEntity(child, parent, (object)"挂接数据"); break;
                        case 20: QGF.Entity.AttachEntity(child, parent, "Socket"); break;
                        case 21: QGF.Entity.AttachEntity(child, parent, "Socket", "挂接数据"); break;
                        case 22: QGF.Entity.AttachEntity(child, parent, socket); break;
                        case 23: QGF.Entity.AttachEntity(child, parent, socket, "挂接数据"); break;
                    }
                });
                detachButton.onClick.AddListener(() =>
                {
                    switch (detachMode.value)
                    {
                        // 解除框架登记的父子关系；基类回调把 Transform 恢复到实体组下。
                        case 0: QGF.Entity.DetachEntity(11); break;
                        case 1: QGF.Entity.DetachEntity(11, "解除数据"); break;
                        case 2: QGF.Entity.DetachEntity(QGF.Entity.GetEntity(11)); break;
                        case 3: QGF.Entity.DetachEntity(QGF.Entity.GetEntity(11), "解除数据"); break;
                        case 4: QGF.Entity.DetachChildEntities(10); break;
                        case 5: QGF.Entity.DetachChildEntities(10, "解除数据"); break;
                        case 6: QGF.Entity.DetachChildEntities(QGF.Entity.GetEntity(10)); break;
                        case 7: QGF.Entity.DetachChildEntities(QGF.Entity.GetEntity(10), "解除数据"); break;
                    }
                });
                // HideEntity 结束显示，实例由框架回池。
                hideParentButton.onClick.AddListener(() => QGF.Entity.HideEntity(10));
                showButton.onClick.AddListener(ShowMissing);
                queryButton.onClick.AddListener(Query);
                ready = true;
                ShowMissing();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Query()
        {
            var parent = QGF.Entity.GetEntity(10);
            var child = QGF.Entity.GetEntity(11);
            // 编号和实例入口查询同一份登记关系；这里只显示按下按钮时的快照。
            var ownerById = QGF.Entity.GetParentEntity(11);
            var ownerByInstance = QGF.Entity.GetParentEntity(child);
            var firstById = QGF.Entity.GetChildEntity(10);
            var firstByInstance = QGF.Entity.GetChildEntity(parent);
            var byId = QGF.Entity.GetChildEntities(10);
            var byInstance = QGF.Entity.GetChildEntities(parent);
            var idList = new List<IEntity>();
            var instanceList = new List<IEntity>();
            QGF.Entity.GetChildEntities(10, idList);
            QGF.Entity.GetChildEntities(parent, instanceList);
            bool same = ownerById == ownerByInstance && firstById == firstByInstance
                && byId.Select(entity => entity.Id).SequenceEqual(byInstance.Select(entity => entity.Id))
                && idList.Select(entity => entity.Id).SequenceEqual(instanceList.Select(entity => entity.Id));
            queryText.text = $"父实体：{(ownerById == null ? "无" : ownerById.Id.ToString())}    首个子实体：{(firstById == null ? "无" : firstById.Id.ToString())}\n"
                + $"数组：{byId.Length}/{byInstance.Length}    List：{idList.Count}/{instanceList.Count}    "
                + $"查询结果：{(same ? "一致" : "不一致")}";
        }

        private void ShowMissing()
        {
            // 已显示与正在加载是两种状态，避免对同一编号重复发起显示。
            if (!QGF.Entity.HasEntity(10) && !QGF.Entity.IsLoadingEntity(10))
                QGF.Entity.ShowEntity<AttachedCubeLogic>(10, asset, "Example", new Vector3(3, 0, 0));
            if (!QGF.Entity.HasEntity(11) && !QGF.Entity.IsLoadingEntity(11))
                QGF.Entity.ShowEntity<AttachedCubeLogic>(11, asset, "Example", new Vector3(4.7f, 1, 0));
        }

        private void Update()
        {
            if (!ready) return;
            var parent = QGF.Entity.GetEntity(10);
            var child = QGF.Entity.GetEntity(11);
            var owner = child == null ? null : QGF.Entity.GetParentEntity(child);
            bool attached = owner != null;
            relationText.text = $"父实体 10：{(parent == null ? "未显示" : "已显示")}\n子实体 11：{(child == null ? "未显示" : attached ? "跟随父实体旋转" : "独立于父实体")}\n"
                + $"登记的子实体：{(parent == null ? 0 : QGF.Entity.GetChildEntityCount(10))}";
            var logic = child == null ? null : (AttachedCubeLogic)child.Logic;
            callbackText.text = logic == null ? "子实体未显示" : $"挂接回调：{logic.AttachCalls}    解除回调：{logic.DetachCalls}\n本次回调数据：{logic.LastData}";
            attachButton.interactable = parent != null && child != null && !attached;
            queryButton.interactable = parent != null && child != null;
            detachButton.interactable = attached;
            hideParentButton.interactable = parent != null;
            // 查询当前已加载或待加载集合；数组和 List 入口都只反映调用时的状态。
            showButton.interactable = QGF.Entity.GetAllLoadingEntityIds().Length == 0 && (parent == null || child == null);
        }

        private void OnDestroy()
        {
            // 父子同时退出，不能只解除 Transform 后把实例留在管理器里。
            var entities = GameEntry.GetComponent<EntityComponent>();
            if (!ready || entities == null) return;
            for (int id = 10; id <= 11; id++)
                if (entities.HasEntity(id) || entities.IsLoadingEntity(id)) entities.HideEntity(id);
        }
    }
}
#endif
