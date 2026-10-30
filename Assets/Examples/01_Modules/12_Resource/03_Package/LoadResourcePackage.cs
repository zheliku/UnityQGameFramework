using TMPro;
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
        [SerializeField] private TMP_Text stateText, assetText, groupsText;
        private AssetHandle handle;
        private GameObject instance;
        private bool initialized, loading;

        private void Start()
        {
            stateText.text = "Initialize the package and load Cube. Try destroying the package while holding its handle.";
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
                    stateText.text = "Package destroyed. It can be initialized again.";
                }
                else
                {
                    await QGF.Resource.InitializePackageAsync(cancellationToken: destroyCancellationToken);
                    initialized = true;
                    stateText.text = "Resource package ready.";
                }
                initializeButton.GetComponentInChildren<TMP_Text>().text = initialized ? "Destroy Package" : "Initialize Package";
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
                // 这里按 YooAsset 地址加载；地址来自 Resource 组的 AddressByGroupAndFileName 规则。
                // 显式句柄只代表本次借用，释放使用 handle.Release()，不再调用 UnloadAsset。
                handle = await QGF.Resource.LoadAssetHandleAsync<GameObject>(ResourceExamplePaths.CubeAddress, cancellationToken: destroyCancellationToken);
                instance = Instantiate((GameObject)handle.AssetObject, new Vector3(3, -2, 0), Quaternion.Euler(20, 35, 0));
                stateText.text = "The handle owns Cube. Destroy Package is rejected while the handle is held.";
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
                stateText.text = "Instance destroyed and handle released. The package can now be destroyed.";
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
            groupsText.text = $"Package: {QGF.Resource.Settings.DefaultPackageName}\nInitialized: {initialized}";
            assetText.text = held ? "Holding an independent asset handle." : "No outstanding asset handles.";
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
