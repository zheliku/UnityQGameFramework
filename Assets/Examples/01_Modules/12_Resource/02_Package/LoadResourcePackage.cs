#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class LoadResourcePackage : MonoBehaviour
    {
        [SerializeField] private Button initializeButton, loadButton, releaseButton;
        [SerializeField] private Text stateText, assetText, groupsText;

        private ResourceFixtureManifest fixture;
        private GameObject asset, instance;
        private bool initialized, loading, releasing;

        private void Start()
        {
            fixture = ResourceExamplePaths.Fixture;
            stateText.text = fixture == null ? ResourceExamplePaths.Notice : "先初始化资源包，再加载 Cube。";
            initializeButton.onClick.AddListener(Initialize);
            loadButton.onClick.AddListener(Load);
            releaseButton.onClick.AddListener(Release);
        }

        private void Initialize()
        {
            loading = true;
            // Package 模式先读取构建好的资源清单。
            QGF.Resource.InitResources(() =>
            {
                if (this == null) return;
                initialized = true;
                loading = false;
                stateText.text = "资源包已就绪。";
            });
        }

        private void Load()
        {
            loading = true;
            var resources = QGF.Resource;
            // LoadAsset 取回预制体资产；Instantiate 创建独立的场景实例。
            resources.LoadAsset(fixture.PrefabAssetName, new LoadAssetCallbacks(
                (name, value, duration, userData) =>
                {
                    if (this == null)
                    {
                        resources.UnloadAsset(value);
                        return;
                    }
                    asset = (GameObject)value;
                    instance = Instantiate(asset, new Vector3(3, -2, 0), Quaternion.Euler(20, 35, 0));
                    loading = false;
                    stateText.text = $"加载完成，用时 {duration:F3} 秒。";
                },
                (name, status, error, userData) =>
                {
                    if (this == null) return;
                    loading = false;
                    stateText.text = "加载失败：" + error;
                }));
        }

        private async void Release()
        {
            var cancellation = destroyCancellationToken;
            releasing = true;
            Destroy(instance);
            try
            {
                // Unity 的 Destroy 在帧末生效，实例销毁后再归还 prefab 资产。
                await UniTask.NextFrame(cancellation);
                QGF.Resource.UnloadAsset(asset);
                asset = null;
                instance = null;
                releasing = false;
                stateText.text = "实例已销毁，资源引用已归还。";
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }

        private void Update()
        {
            initializeButton.interactable = fixture != null && !initialized && !loading;
            loadButton.interactable = initialized && asset == null && !loading && !releasing;
            releaseButton.interactable = asset != null && !releasing;

            groupsText.text = initialized
                ? $"模式：{QGF.Resource.ResourceMode}\n资产：{QGF.Resource.AssetCount}  资源：{QGF.Resource.ResourceCount}"
                : "资源包尚未初始化。";
            assetText.text = asset == null ? "Cube 资源未加载。" : "Cube 资源已加载并创建场景实例。";
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
