using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 通过回调加载一个预制体资产并创建两个实例。
    /// </summary>
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
            // GUID 来自收集清单；LoadAsset 只借用资产，不创建实例。
            resources.LoadAsset(prefabGuid, typeof(GameObject), new LoadAssetCallbacks(
                (name, value, duration, userData) =>
                {
                    if (this == null)
                    {
                        resources.UnloadAsset(value);
                        return;
                    }
                    asset = (GameObject)value;
                    first = Instantiate(asset, new Vector3(2, -1, 0), Quaternion.Euler(20, 35, 0));
                    second = Instantiate(asset, new Vector3(4, 1, 0), Quaternion.Euler(20, 35, 0));
                    stateText.text = "一个资产，两个实例。销毁左边，观察右边是否还在。";
                },
                (name, status, message, userData) =>
                {
                    if (this != null)
                    {
                        stateText.text = "加载失败：" + message;
                    }
                }));
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
                stateText.text = "两个实例已销毁，资产引用已归还";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
        }

        private void Update()
        {
            assetText.text = asset == null ? "未持有资产引用" : $"资产：{asset.name}（{asset.GetInstanceID()}）";
            instancesText.text = $"左边：{(first == null ? "无" : first.GetInstanceID().ToString())}\n右边：{(second == null ? "无" : second.GetInstanceID().ToString())}";
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
                resources.UnloadAsset(asset);
            }
        }
    }
}
