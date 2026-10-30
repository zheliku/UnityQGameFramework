using TMPro;
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 异步加载一个预制体资产并创建两个实例。
    /// </summary>
    public sealed class LoadCubePrefab : MonoBehaviour
    {
        [SerializeField] private TMP_Text stateText, assetText, instancesText;
        [SerializeField] private Button removeButton, releaseButton;
        private GameObject asset, first, second;
        private bool releasing;

        private async void Start()
        {
            stateText.text = "Loading prefab";
            removeButton.onClick.AddListener(() => Destroy(first));
            releaseButton.onClick.AddListener(Release);
            var resources = QGF.Resource;
            // 以当前场景目录拼出路径，兼容 Assets/Examples 和 Package Manager 的 Sample 导入目录。
            string resourceDirectory = Path.GetDirectoryName(Path.GetDirectoryName(gameObject.scene.path));
            string assetPath = resourceDirectory.Replace('\\', '/') + "/Common/Art/Cube.prefab";

            try
            {
                // 异步加载会先确保默认包已初始化；本次借用保留到两个实例都销毁后再归还。
                asset = await resources.LoadAssetAsync<GameObject>(assetPath, cancellationToken: destroyCancellationToken);
                first = Instantiate(asset, new Vector3(2, -1, 0), Quaternion.Euler(20, 35, 0));
                second = Instantiate(asset, new Vector3(4, 1, 0), Quaternion.Euler(20, 35, 0));
                stateText.text = "One asset, two instances. Destroy the left instance and observe the right.\nPath: " + assetPath;
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                stateText.text = "Load failed: " + exception.Message;
            }
        }

        private async void Release()
        {
            releasing = true;
            Destroy(first);
            Destroy(second);
            try
            {
                // Destroy 在帧末生效；实例结束使用后，才归还预制体资产。
                await UniTask.NextFrame(destroyCancellationToken);
                QGF.Resource.UnloadAsset(asset);
                asset = null;
                stateText.text = "Both instances destroyed and asset reference released.";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
        }

        private void Update()
        {
            assetText.text = asset == null ? "No asset reference held" : $"Asset: {asset.name}（{asset.GetInstanceID()}）";
            instancesText.text = $"Left: {(first == null ? "None" : first.GetInstanceID().ToString())}\nRight: {(second == null ? "None" : second.GetInstanceID().ToString())}";
            removeButton.interactable = first != null && !releasing;
            releaseButton.interactable = asset != null && !releasing;
        }

        private void OnDestroy()
        {
            if (first != null)
            {
                Destroy(first);
            }
            if (second != null)
            {
                Destroy(second);
            }
            var resources = GameEntry.GetComponent<ResourceComponent>();
            if (resources != null && asset != null)
            {
                try
                {
                    resources.UnloadAsset(asset);
                }
                catch (OperationCanceledException)
                {
                    // 框架退出时资源后端可能已经关闭，借用会随资源服务一起清理。
                }
            }
        }
    }
}
