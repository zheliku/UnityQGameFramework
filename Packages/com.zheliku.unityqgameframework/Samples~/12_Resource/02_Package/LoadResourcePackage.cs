#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class LoadResourcePackage : MonoBehaviour
    {
        [SerializeField] private Button initializeButton, loadButton, releaseButton, cleanupButton;
        [SerializeField] private Dropdown loadMode, groupMode;
        [SerializeField] private Toggle listToggle, gcToggle;
        [SerializeField] private Text stateText, assetText, groupsText, tasksText;
        private readonly List<TaskInfo> tasks = new List<TaskInfo>();
        private readonly List<IResourceGroup> groups = new List<IResourceGroup>();
        private readonly List<string> resourceNames = new List<string>();
        private readonly List<string> groupNames = new List<string>();
        private ResourceFixtureManifest fixture;
        private GameObject asset, instance;
        private bool initialized, loading, releasing, ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                fixture = ResourceExamplePaths.Fixture;
                stateText.text = fixture == null ? ResourceExamplePaths.Notice : "点击初始化，读取真正的包内资源清单。";
                initializeButton.onClick.AddListener(() =>
                {
                    loading = true;
                    // Package 初始化读取构建后的版本清单，完成后才能加载资产。
                    QGF.Resource.InitResources(() =>
                    {
                        if (this == null) return;
                        initialized = true;
                        loading = false;
                        RefreshGroups();
                        stateText.text = $"Package 已初始化，资产 {QGF.Resource.AssetCount} 个，资源 {QGF.Resource.ResourceCount} 个。";
                    });
                });
                loadButton.onClick.AddListener(Load);
                releaseButton.onClick.AddListener(Release);
                cleanupButton.onClick.AddListener(() => QGF.Resource.ForceUnloadUnusedAssets(gcToggle.isOn));
                groupMode.onValueChanged.AddListener(_ => RefreshGroups());
                listToggle.onValueChanged.AddListener(_ => RefreshGroups());
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Load()
        {
            loading = true;
            stateText.text = "从 AssetBundle 加载 Cube…";
            var resources = QGF.Resource;
            var callbacks = new LoadAssetCallbacks((name, value, duration, context) =>
            {
                if (this == null) { if (resources != null) resources.UnloadAsset(value); return; }
                asset = (GameObject)value;
                instance = Instantiate(asset, new Vector3(3, -2, 0), Quaternion.Euler(20, 35, 0));
                loading = false;
                stateText.text = $"加载完成，用时 {duration:F3} 秒；上下文匹配 {ReferenceEquals(context, this)}。";
            }, (name, status, message, context) =>
            {
                if (this == null) return;
                loading = false;
                stateText.text = "加载失败：" + message;
            });
            // 八个重载只改变类型约束、调度优先级和回调上下文，加载的是同一个资产。
            switch (loadMode.value)
            {
                case 0: resources.LoadAsset(fixture.PrefabAssetName, callbacks); break;
                case 1: resources.LoadAsset(fixture.PrefabAssetName, typeof(GameObject), callbacks); break;
                case 2: resources.LoadAsset(fixture.PrefabAssetName, 10, callbacks); break;
                case 3: resources.LoadAsset(fixture.PrefabAssetName, callbacks, this); break;
                case 4: resources.LoadAsset(fixture.PrefabAssetName, typeof(GameObject), 10, callbacks); break;
                case 5: resources.LoadAsset(fixture.PrefabAssetName, typeof(GameObject), callbacks, this); break;
                case 6: resources.LoadAsset(fixture.PrefabAssetName, 10, callbacks, this); break;
                default: resources.LoadAsset(fixture.PrefabAssetName, typeof(GameObject), 10, callbacks, this); break;
            }
        }

        private async void Release()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                releasing = true;
                Destroy(instance);
                // 场景实例先结束使用，再把资产引用还给资源池。
                await UniTask.NextFrame(cancellation);
                QGF.Resource.UnloadAsset(asset);
                asset = null;
                releasing = false;
                stateText.text = "实例已销毁，资产引用已归还，可以再次加载。";
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!ready) return;
            initializeButton.interactable = fixture != null && !initialized && !loading;
            loadButton.interactable = initialized && !loading && asset == null && !releasing;
            releaseButton.interactable = asset != null && !releasing;
            cleanupButton.interactable = initialized && !loading && !releasing;
            assetText.text = !initialized ? "资源尚未初始化。"
                : $"资源清单：{ResourceExamplePaths.GetAssetStatusText(QGF.Resource.HasAsset(fixture.PrefabAssetName))}\n"
                + $"模式：{QGF.Resource.ResourceMode}  只读：{Path.GetRelativePath(ResourceExamplePaths.BuildRoot, QGF.Resource.ReadOnlyPath)}\n"
                + $"读写：{Path.GetRelativePath(ResourceExamplePaths.BuildRoot, QGF.Resource.ReadWritePath)}\n"
                + (instance == null ? "当前无 Cube 实例。" : $"场景实例 {instance.GetInstanceID()}    资产 {asset.GetInstanceID()}");
            if (!initialized) return;
            if (listToggle.isOn) QGF.Resource.GetAllLoadAssetInfos(tasks);
            else { tasks.Clear(); tasks.AddRange(QGF.Resource.GetAllLoadAssetInfos()); }
            tasksText.text = $"等待任务 {tasks.Count}    空闲代理 {QGF.Resource.LoadFreeAgentCount}/{QGF.Resource.LoadTotalAgentCount}\n"
                + $"距上次清理 {QGF.Resource.LastUnloadUnusedAssetsOperationElapseSeconds:F1} 秒";
        }

        private void RefreshGroups()
        {
            if (!initialized) return;
            var resources = QGF.Resource;
            if (listToggle.isOn) resources.GetAllResourceGroups(groups);
            else { groups.Clear(); groups.AddRange(resources.GetAllResourceGroups()); }
            if (groupMode.value == 3 || groupMode.value == 4)
            {
                groupNames.Clear();
                groupNames.Add("Base");
                groupNames.Add(groupMode.value == 3 ? "Data" : "Shared");
                // Shared 含 Cube 和 Archive，与 Base 重叠一个 Cube；集合内只统计两份资源。
                var collection = listToggle.isOn ? resources.GetResourceGroupCollection(groupNames)
                    : resources.GetResourceGroupCollection(groupNames.ToArray());
                if (listToggle.isOn) collection.GetResourceNames(resourceNames);
                else { resourceNames.Clear(); resourceNames.AddRange(collection.GetResourceNames()); }
                groupsText.text = $"集合包含 {collection.GetResourceGroups().Length} 组，资源 {collection.ReadyCount}/{collection.TotalCount}\n"
                    + $"集合状态：{ResourceExamplePaths.GetReadinessText(collection.Ready)}；进度 {collection.Progress:P0}\n"
                    + $"资源大小 {collection.ReadyLength}/{collection.TotalLength} 字节\n"
                    + $"压缩大小 {collection.ReadyCompressedLength}/{collection.TotalCompressedLength} 字节\n"
                    + string.Join("、", resourceNames);
                return;
            }
            string name = groupMode.value == 0 ? ""
                : groupMode.value == 1 ? "Base"
                : groupMode.value == 2 ? "Data"
                : groupMode.value == 6 ? "Shared"
                : "Missing";
            // 先确认名称已注册，再取资源组，查询失败时界面不继续解引用。
            if (!resources.HasResourceGroup(name)) { groupsText.text = $"资源组 {name} 未注册。"; return; }
            var group = name.Length == 0 ? resources.GetResourceGroup() : resources.GetResourceGroup(name);
            if (listToggle.isOn) group.GetResourceNames(resourceNames);
            else { resourceNames.Clear(); resourceNames.AddRange(group.GetResourceNames()); }
            // Package 的清单按本地资源长度登记压缩大小；这里两种大小相同。
            groupsText.text = $"共 {resources.ResourceGroupCount} 个资源组\n组 {(group.Name.Length == 0 ? "全部" : group.Name)}：{group.ReadyCount}/{group.TotalCount}\n"
                + $"资源组状态：{ResourceExamplePaths.GetReadinessText(group.Ready)}；进度 {group.Progress:P0}\n"
                + $"资源大小 {group.ReadyLength}/{group.TotalLength} 字节\n"
                + $"压缩大小 {group.ReadyCompressedLength}/{group.TotalCompressedLength} 字节\n" + string.Join("、", resourceNames);
        }

        private void OnDestroy()
        {
            if (instance != null) Destroy(instance);
            var resources = GameEntry.GetComponent<ResourceComponent>();
            if (resources != null && asset != null) resources.UnloadAsset(asset);
        }
    }
}
#endif
