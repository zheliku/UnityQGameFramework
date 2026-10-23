#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.ObjectPool;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class InspectResourceCache : MonoBehaviour
    {
        [SerializeField] private Button initializeButton, loadButton, returnButton, reserveButton, forceButton;
        [SerializeField] private Dropdown policyMode;
        [SerializeField] private Slider minimumSlider, maximumSlider;
        [SerializeField] private Toggle gcToggle;
        [SerializeField] private Text stateText, poolText, policyText, cleanupText;
        [SerializeField] private Text minimumValueText, maximumValueText;
        private ResourceFixtureManifest fixture;
        private ObjectPoolBase assetPool, resourcePool;
        private GameObject asset, instance;
        private bool ready, initialized, busy;
        private int previousAssetId, currentAssetId, cleanupCount;
        private float previousCleanupElapsed;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                fixture = ResourceExamplePaths.Fixture;
                stateText.text = fixture == null ? ResourceExamplePaths.Notice : "初始化后加载 Cube，再归还引用，观察缓存数量。";
                initializeButton.onClick.AddListener(() =>
                {
                    busy = true;
                    QGF.Resource.InitResources(() =>
                    {
                        if (this == null) return;
                        // 只读取 Resource 创建的两个池，释放策略通过 Resource 的公开参数调整。
                        assetPool = QGF.ObjectPool.GetObjectPool(pool => pool.Name == "Asset Pool");
                        resourcePool = QGF.ObjectPool.GetObjectPool(pool => pool.Name == "Resource Pool");
                        initialized = true;
                        busy = false;
                        ApplyPolicy();
                        stateText.text = "已初始化，当前尚未加载 Cube。";
                    });
                });
                loadButton.onClick.AddListener(Load);
                returnButton.onClick.AddListener(Return);
                policyMode.onValueChanged.AddListener(_ => { if (initialized) ApplyPolicy(); });
                minimumSlider.onValueChanged.AddListener(value => QGF.Resource.MinUnloadUnusedAssetsInterval = value);
                maximumSlider.onValueChanged.AddListener(value => QGF.Resource.MaxUnloadUnusedAssetsInterval = value);
                QGF.Resource.MinUnloadUnusedAssetsInterval = minimumSlider.value;
                QGF.Resource.MaxUnloadUnusedAssetsInterval = maximumSlider.value;
                reserveButton.onClick.AddListener(() =>
                {
                    // 预订 Unity 清理，达到最小间隔才执行。它不会把 QGF 池中的缓存归还。
                    QGF.Resource.UnloadUnusedAssets(gcToggle.isOn);
                    stateText.text = "已预订 Unity 清理，等待最小间隔。QGF 缓存仍按池策略回收。";
                });
                forceButton.onClick.AddListener(() =>
                {
                    // 强制清理跳过最小间隔；加载中和仍在使用的资产不应由业务强行卸载。
                    QGF.Resource.ForceUnloadUnusedAssets(gcToggle.isOn);
                    stateText.text = "已请求立即执行 Unity 清理。";
                });
                previousCleanupElapsed = QGF.Resource.LastUnloadUnusedAssetsOperationElapseSeconds;
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void ApplyPolicy()
        {
            var resources = QGF.Resource;
            bool expire = policyMode.value == 2;
            // 容量是缓存的回收目标，不限制正在使用的对象数量，也不会提前创建资产。
            resources.AssetAutoReleaseInterval = resources.ResourceAutoReleaseInterval = expire ? .25f : 30f;
            resources.AssetExpireTime = resources.ResourceExpireTime = expire ? 2f : 30f;
            resources.AssetCapacity = resources.ResourceCapacity = policyMode.value == 1 ? 0 : 4;
            // 池优先级用于管理器排序；资产加载的调度优先级由 LoadAsset 的参数决定。
            resources.AssetPriority = 10;
            resources.ResourcePriority = 20;
        }

        private void Load()
        {
            busy = true;
            var resources = QGF.Resource;
            resources.LoadAsset(fixture.PrefabAssetName, typeof(GameObject), new LoadAssetCallbacks(
                (name, value, duration, context) =>
                {
                    if (this == null) { if (resources != null) resources.UnloadAsset(value); return; }
                    asset = (GameObject)value;
                    previousAssetId = currentAssetId;
                    currentAssetId = asset.GetInstanceID();
                    instance = Instantiate(asset, new Vector3(3, -3.4f, 0), Quaternion.Euler(20, 35, 0));
                    busy = false;
                    stateText.text = $"当前资产 ID {currentAssetId}，上次 {previousAssetId}\n场景实例 ID {instance.GetInstanceID()}";
                },
                (name, status, message, context) =>
                {
                    if (this == null) return;
                    busy = false;
                    stateText.text = "加载失败：" + message;
                }));
        }

        private async void Return()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                busy = true;
                Destroy(instance);
                // 先销毁业务实例，下一帧再归还资产引用；UnloadAsset 归还后可能继续保留缓存。
                await UniTask.NextFrame(cancellation);
                QGF.Resource.UnloadAsset(asset);
                asset = null;
                busy = false;
                stateText.text = "实例已销毁，资产引用已归还。查看池中是否仍保留缓存。";
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }

        private void Update()
        {
            if (!ready) return;
            initializeButton.interactable = fixture != null && !initialized && !busy;
            loadButton.interactable = initialized && !busy && asset == null;
            returnButton.interactable = asset != null && !busy;
            policyMode.interactable = !busy;
            reserveButton.interactable = forceButton.interactable = initialized && !busy;
            var resources = QGF.Resource;
            float elapsed = resources.LastUnloadUnusedAssetsOperationElapseSeconds;
            if (elapsed < previousCleanupElapsed) cleanupCount++;
            previousCleanupElapsed = elapsed;
            minimumValueText.text = $"最小间隔 {resources.MinUnloadUnusedAssetsInterval:F1} 秒";
            maximumValueText.text = $"最大间隔 {resources.MaxUnloadUnusedAssetsInterval:F1} 秒";
            cleanupText.text = $"距上次 Unity 清理 {elapsed:F1} 秒，已观察到 {cleanupCount} 次清理\n"
                + $"加载代理：工作 {resources.LoadWorkingAgentCount}，等待任务 {resources.LoadWaitingTaskCount}";
            if (!initialized) return;
            int assetSpawns = 0, resourceSpawns = 0;
            foreach (var info in assetPool.GetAllObjectInfos()) assetSpawns += info.SpawnCount;
            foreach (var info in resourcePool.GetAllObjectInfos()) resourceSpawns += info.SpawnCount;
            poolText.text = $"资产池：缓存 {assetPool.Count}，借用 {assetSpawns}，可回收 {assetPool.CanReleaseCount}\n"
                + $"资源池：缓存 {resourcePool.Count}，借用 {resourceSpawns}，可回收 {resourcePool.CanReleaseCount}";
            policyText.text = $"资产池：容量 {resources.AssetCapacity}，过期 {resources.AssetExpireTime:F1} 秒\n"
                + $"每 {resources.AssetAutoReleaseInterval:F2} 秒检查，池优先级 {resources.AssetPriority}\n"
                + $"资源池：容量 {resources.ResourceCapacity}，过期 {resources.ResourceExpireTime:F1} 秒\n"
                + $"每 {resources.ResourceAutoReleaseInterval:F2} 秒检查，池优先级 {resources.ResourcePriority}";
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
