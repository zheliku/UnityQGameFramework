#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class LoadCubePrefab : MonoBehaviour
    {
        [SerializeField] private Text stateText, assetText, instancesText;
        [SerializeField] private Button removeButton, releaseButton;
        [SerializeField] private string prefabGuid;
        private GameObject asset, first, second;
        private bool releasing;

        private void Start()
        {
            stateText.text = "正在加载预制体";
            removeButton.onClick.AddListener(() => Destroy(first));
            releaseButton.onClick.AddListener(Release);
            var resources = QGF.Resource;
            // LoadAsset 取得预制体资产，Instantiate 创建场景实例。
            resources.LoadAsset(UnityEditor.AssetDatabase.GUIDToAssetPath(prefabGuid), new LoadAssetCallbacks(
                (name, value, duration, data) =>
                {
                    // 资源请求可能在场景退出后完成。此时归还引用，不再创建物体。
                    if (this == null)
                    {
                        if (resources != null) resources.UnloadAsset(value);
                        return;
                    }
                    asset = (GameObject)value;
                    first = Instantiate(asset, new Vector3(2, -1, 0), Quaternion.Euler(20, 35, 0));
                    second = Instantiate(asset, new Vector3(4, 1, 0), Quaternion.Euler(20, 35, 0));
                    stateText.text = "一个资产，两个实例。销毁左边，观察右边是否还在。";
                },
                (name, status, message, data) =>
                {
                    if (this != null) stateText.text = "加载失败：" + message;
                }));
        }

        private async void Release()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                releasing = true;
                Destroy(first);
                Destroy(second);
                // Destroy 在帧末生效；所有实例结束使用后，再归还资产引用。
                await UniTask.NextFrame(cancellation);
                QGF.Resource.UnloadAsset(asset);
                asset = null;
                stateText.text = "两个实例已销毁，资产引用已归还";
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            assetText.text = asset == null ? "本示例未持有资产引用" : $"资产：{asset.name}（{asset.GetInstanceID()}）";
            instancesText.text = $"左边：{(first == null ? "无" : first.GetInstanceID().ToString())}\n"
                + $"右边：{(second == null ? "无" : second.GetInstanceID().ToString())}";
            removeButton.interactable = first != null && !releasing;
            releaseButton.interactable = asset != null && !releasing;
        }

        private void OnDestroy()
        {
            if (first != null) Destroy(first);
            if (second != null) Destroy(second);
            var resources = GameEntry.GetComponent<ResourceComponent>();
            if (resources != null && asset != null) resources.UnloadAsset(asset);
        }
    }
}
#endif
