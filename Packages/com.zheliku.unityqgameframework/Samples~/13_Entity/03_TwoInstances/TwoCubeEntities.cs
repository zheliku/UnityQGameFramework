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
    public sealed class TwoCubeEntities : MonoBehaviour
    {
        [SerializeField] private string prefabGuid;
        [SerializeField] private Dropdown queryMode;
        [SerializeField] private Toggle contextToggle;
        [SerializeField] private Button showButton, hideFirstButton, hideAllButton, queryButton;
        [SerializeField] private Text countsText, queryText;
        [SerializeField] private Dropdown groupDropdown;
        [SerializeField] private InputField entityIdInput;
        [SerializeField] private Button queryGroupButton;
        [SerializeField] private Text groupQueryText;
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
                QGF.Entity.AddEntityGroup("Left", 30f, 8, 60f, 0);
                QGF.Entity.AddEntityGroup("Right", 30f, 8, 60f, 0);
                queryMode.ClearOptions();
                queryMode.AddOptions(new List<string>
                {
                    "所有实体 · 数组", "所有实体 · List", "同一资源 · 数组", "同一资源 · List",
                    "实体组 · 数组", "实体组 · List", "同一资源的首个实体",
                    "待加载编号 · 数组", "待加载编号 · List"
                });
                groupDropdown.ClearOptions();
                groupDropdown.AddOptions(new List<string> { "Left", "Right" });
                queryGroupButton.onClick.AddListener(QueryGroup);
                groupQueryText.text = "选择实体组和编号，再查询组内已显示的实体。";
                showButton.onClick.AddListener(ShowMissing);
                // HideEntity 结束显示，实例由框架回池。
                hideFirstButton.onClick.AddListener(() => QGF.Entity.HideEntity(1));
                hideAllButton.onClick.AddListener(() =>
                {
                    // 两个重载都只隐藏已显示实体；带参重载把数据传给 OnHide。
                    if (!contextToggle.isOn) QGF.Entity.HideAllLoadedEntities();
                    else QGF.Entity.HideAllLoadedEntities(this);
                });
                queryButton.onClick.AddListener(() =>
                {
                    var entities = new List<Entity>();
                    var groups = new List<IEntityGroup>();
                    var loading = new List<int>();
                    switch (queryMode.value)
                    {
                        // 查询当前已加载或待加载集合；数组和 List 入口都只反映调用时的状态。
                        case 0: entities.AddRange(QGF.Entity.GetAllLoadedEntities()); break;
                        case 1: QGF.Entity.GetAllLoadedEntities(entities); break;
                        case 2: entities.AddRange(QGF.Entity.GetEntities(asset)); break;
                        case 3: QGF.Entity.GetEntities(asset, entities); break;
                        case 4: groups.AddRange(QGF.Entity.GetAllEntityGroups()); break;
                        case 5: QGF.Entity.GetAllEntityGroups(groups); break;
                        case 6:
                            // GetEntity 取得已显示的实体，未显示时返回 null。
                            var first = QGF.Entity.GetEntity(asset);
                            if (first != null) entities.Add(first);
                            break;
                        case 7: loading.AddRange(QGF.Entity.GetAllLoadingEntityIds()); break;
                        case 8: QGF.Entity.GetAllLoadingEntityIds(loading); break;
                    }
                    queryText.text = queryMode.value >= 7 ? "待加载编号：" + (loading.Count == 0 ? "无" : string.Join(", ", loading))
                        : queryMode.value < 4 || queryMode.value == 6
                        ? "查询快照：\n" + string.Join("\n", entities.Select(entity => $"实体 {entity.Id}    {entity.EntityGroup.Name} 组"))
                        : "查询快照：\n" + string.Join("\n", groups.Select(group => $"{group.Name}：{group.EntityCount} 个，容量 {group.InstanceCapacity}"));
                });
                ready = true;
                ShowMissing();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void QueryGroup()
        {
            if (!int.TryParse(entityIdInput.text, out int id))
            {
                groupQueryText.text = "实体编号请输入整数。";
                return;
            }

            // 组件返回 Core 的组对象；以下查询只在所选组的已显示实体中查找。
            IEntityGroup group = QGF.Entity.GetEntityGroup(groupDropdown.options[groupDropdown.value].text);
            var byId = group.GetEntity(id);
            var byAsset = group.GetEntity(asset);
            bool hasId = group.HasEntity(id);
            bool hasAsset = group.HasEntity(asset);
            var all = group.GetAllEntities();
            var sameAsset = group.GetEntities(asset);

            // 组查询返回 IEntity。List 重载会先清空结果，可以复用同一个 List。
            var results = new List<IEntity>();
            group.GetAllEntities(results);
            string allList = string.Join(", ", results.Select(entity => entity.Id));
            group.GetEntities(asset, results);

            groupQueryText.text = $"{group.Name} 组 · 查询快照\n"
                + $"编号 {id}：{(hasId ? "存在" : "不存在")}\n资源：{(hasAsset ? "存在" : "不存在")}\n"
                + $"按编号：{byId?.Id.ToString() ?? "无"}；按资源首个：{byAsset?.Id.ToString() ?? "无"}\n"
                + $"全部：数组 [{string.Join(", ", all.Select(entity => entity.Id))}] / List [{allList}]\n"
                + $"同资源：数组 [{string.Join(", ", sameAsset.Select(entity => entity.Id))}] / List [{string.Join(", ", results.Select(entity => entity.Id))}]";
        }

        private void ShowMissing()
        {
            // 同一资产可生成多个独立实体；编号在管理器内唯一，实体组决定实例缓存归属。
            if (!QGF.Entity.HasEntity(1) && !QGF.Entity.IsLoadingEntity(1))
                QGF.Entity.ShowEntity<TwoCubeEntitiesLogic>(1, asset, "Left", new Vector3(2, -1, 0));
            if (!QGF.Entity.HasEntity(2) && !QGF.Entity.IsLoadingEntity(2))
                QGF.Entity.ShowEntity<TwoCubeEntitiesLogic>(2, asset, "Right", new Vector3(4, 1, 0));
        }

        private void Update()
        {
            if (!ready) return;
            // 取得实体组后可以查看本组数量与实例缓存策略。
            var left = QGF.Entity.GetEntityGroup("Left");
            var right = QGF.Entity.GetEntityGroup("Right");
            countsText.text = $"已显示：{QGF.Entity.EntityCount}    实体组：{QGF.Entity.EntityGroupCount}\n"
                // 已显示与正在加载是两种状态，避免对同一编号重复发起显示。
                + $"Left：{left.EntityCount}    Right：{right.EntityCount}\n"
                + $"同一资源：{(QGF.Entity.HasEntity(asset) ? "仍有实例" : "没有实例")}";
            bool loading = QGF.Entity.GetAllLoadingEntityIds().Length > 0;
            hideFirstButton.interactable = left.HasEntity(1);
            hideAllButton.interactable = !loading && QGF.Entity.EntityCount > 0;
            showButton.interactable = !loading && QGF.Entity.EntityCount < 2;
        }

        private void OnDestroy()
        {
            // 不清空整个管理器，只取消和隐藏本示例创建的两个编号。
            var entities = GameEntry.GetComponent<EntityComponent>();
            if (!ready || entities == null) return;
            for (int id = 1; id <= 2; id++)
                if (entities.HasEntity(id) || entities.IsLoadingEntity(id)) entities.HideEntity(id);
        }
    }
}
#endif
