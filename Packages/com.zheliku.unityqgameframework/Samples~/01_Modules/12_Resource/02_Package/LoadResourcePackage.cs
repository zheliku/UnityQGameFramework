using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;
using YooAsset;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 初始化资源包并显式持有、释放资产句柄。
    /// </summary>
    public sealed class LoadResourcePackage : MonoBehaviour
    {
        [SerializeField] private Button initializeButton, loadButton, releaseButton;
        [SerializeField] private Text stateText, assetText, groupsText;
        private AssetHandle handle;
        private GameObject instance;
        private bool initialized, loading;

        private void Start()
        {
            stateText.text = "初始化包后加载 Cube；持有句柄时，尝试销毁包。";
            initializeButton.onClick.AddListener(InitializeOrDestroy);
            loadButton.onClick.AddListener(Load);
            releaseButton.onClick.AddListener(Release);
        }

        private async void InitializeOrDestroy()
        {
            loading = true;
            try
            {
                if (initialized)
                {
                    // 活跃句柄属于包；归还之前销毁包会失败，不会误卸载正在使用的资源。
                    await QGF.Resource.DestroyPackageAsync(cancellationToken: destroyCancellationToken);
                    initialized = false;
                    stateText.text = "包已销毁，可以再次初始化。";
                }
                else
                {
                    await QGF.Resource.InitializePackageAsync(cancellationToken: destroyCancellationToken);
                    initialized = true;
                    stateText.text = "资源包已就绪。";
                }
                initializeButton.GetComponentInChildren<Text>().text = initialized ? "销毁包" : "初始化包";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                stateText.text = exception.Message;
            }
            finally
            {
                loading = false;
            }
        }

        private async void Load()
        {
            loading = true;
            try
            {
                // 显式句柄只代表本次借用，释放使用 handle.Release()，不再调用 UnloadAsset。
                handle = await QGF.Resource.LoadAssetHandleAsync<GameObject>(ResourceExamplePaths.CubeGuid, cancellationToken: destroyCancellationToken);
                instance = Instantiate((GameObject)handle.AssetObject, new Vector3(3, -2, 0), Quaternion.Euler(20, 35, 0));
                stateText.text = "句柄持有 Cube。此时点击“销毁包”会被拒绝。";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                stateText.text = exception.Message;
            }
            finally
            {
                loading = false;
            }
        }

        private async void Release()
        {
            loading = true;
            Destroy(instance);
            try
            {
                await UniTask.NextFrame(destroyCancellationToken);
                handle.Release();
                handle = null;
                stateText.text = "实例已销毁，句柄已释放；现在可以销毁包。";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                loading = false;
            }
        }

        private void Update()
        {
            bool held = handle != null && handle.IsValid;
            initializeButton.interactable = !loading;
            loadButton.interactable = initialized && !held && !loading;
            releaseButton.interactable = held && !loading;
            groupsText.text = $"包：{QGF.Resource.Settings.DefaultPackageName}\n初始化：{initialized}";
            assetText.text = held ? "持有一个独立资产句柄。" : "没有未归还的资产句柄。";
        }

        private void OnDestroy()
        {
            if (instance != null)
            {
                Destroy(instance);
            }
            if (handle != null && handle.IsValid)
            {
                handle.Release();
            }
        }
    }
}
