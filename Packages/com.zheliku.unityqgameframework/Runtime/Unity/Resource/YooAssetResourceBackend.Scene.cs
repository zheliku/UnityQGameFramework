using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Resource;
using YooAsset;
using SceneHandle = YooAsset.SceneHandle;

namespace UnityQGameFramework.Runtime
{
    internal sealed partial class YooAssetResourceBackend
    {
        internal string GetLoadedSceneName(string sceneAssetName, string packageName)
        {
            if (!m_Packages.TryGetValue(GetPackageName(packageName), out PackageInfo package)
                || !package.Scenes.TryGetValue(sceneAssetName, out SceneInfo scene)
                || !scene.Loaded || scene.Handle == null || !scene.Handle.IsValid)
            {
                return null;
            }
            return scene.Handle.SceneName;
        }

        /// <summary>
        /// 异步加载默认资源包中的场景。
        /// </summary>
        /// <param name="sceneAssetName">场景定位地址。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="callbacks">加载回调。</param>
        /// <param name="userData">用户数据。</param>
        public void LoadScene(string sceneAssetName, int priority, LoadSceneCallbacks callbacks, object userData)
        {
            if (callbacks == null)
            {
                throw new FrameworkException("Load scene callbacks is invalid.");
            }
            LoadSceneCallbackAsync(sceneAssetName, priority, callbacks, userData).Forget();
        }

        /// <summary>
        /// 异步卸载默认资源包中的场景。
        /// </summary>
        /// <param name="sceneAssetName">场景定位地址。</param>
        /// <param name="callbacks">卸载回调。</param>
        /// <param name="userData">用户数据。</param>
        public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks callbacks, object userData)
        {
            if (callbacks == null)
            {
                throw new FrameworkException("Unload scene callbacks is invalid.");
            }
            UnloadSceneCallbackAsync(sceneAssetName, callbacks, userData).Forget();
        }

        internal async UniTask LoadSceneAsync(string sceneAssetName, string packageName, int priority, Action<float> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new FrameworkException("Scene asset name is invalid.");
            }
            PackageInfo package = CreatePackage(packageName);
            if (package.Scenes.ContainsKey(sceneAssetName))
            {
                throw new FrameworkException("Scene is already loaded or loading: " + sceneAssetName);
            }

            var scene = new SceneInfo();
            package.Scenes.Add(sceneAssetName, scene);
            package.Requests++;
            m_LoadingCount++;
            bool succeeded = false;
            try
            {
                using (var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_ShutdownToken))
                {
                    await InitializePackageAsync(package.Package.PackageName, cancellation.Token);
                    cancellation.Token.ThrowIfCancellationRequested();
                    // 始终叠加加载，保留 QGF 所在的启动场景。
                    AssetInfo sceneAsset = GetSceneAssetInfo(package.Package, sceneAssetName);
                    scene.Handle = package.Package.LoadSceneAsync(sceneAsset, LoadSceneMode.Additive, LocalPhysicsMode.None, true, (uint)Math.Max(priority, 0));
                    while (scene.Handle.IsValid && !scene.Handle.IsDone)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        progress?.Invoke(scene.Handle.Progress);
                        await UniTask.NextFrame(cancellation.Token);
                    }
                    cancellation.Token.ThrowIfCancellationRequested();
                    if (!scene.Handle.IsValid || scene.Handle.Status != EOperationStatus.Succeeded)
                    {
                        throw new FrameworkException(scene.Handle.IsValid ? scene.Handle.Error : "The scene handle was released during loading.");
                    }
                    progress?.Invoke(1f);
                    cancellation.Token.ThrowIfCancellationRequested();
                    scene.Loaded = true;
                    succeeded = true;
                }
            }
            finally
            {
                try
                {
                    if (!succeeded)
                    {
                        // Unity 场景加载不能直接中止；等加载结束后补偿卸载，再结束本请求。
                        if (scene.Handle != null && scene.Handle.IsValid)
                        {
                            while (scene.Handle.IsValid && !scene.Handle.IsDone)
                            {
                                await UniTask.NextFrame();
                            }
                            if (scene.Handle.IsValid && scene.Handle.Status == EOperationStatus.Succeeded)
                            {
                                await ResourceOperation.WaitAsync(scene.Handle.UnloadSceneAsync(), CancellationToken.None);
                            }
                            else if (scene.Handle.IsValid)
                            {
                                scene.Handle.Release();
                            }
                        }
                        package.Scenes.Remove(sceneAssetName);
                    }
                }
                finally
                {
                    package.Requests--;
                    m_LoadingCount--;
                }
            }
        }

        internal async UniTask UnloadSceneAsync(string sceneAssetName, string packageName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            if (!m_Packages.TryGetValue(GetPackageName(packageName), out PackageInfo package)
                || !package.Scenes.TryGetValue(sceneAssetName, out SceneInfo scene) || !scene.Loaded || scene.Unloading)
            {
                throw new FrameworkException("Scene is not loaded or is already unloading: " + sceneAssetName);
            }

            scene.Unloading = true;
            package.Requests++;
            m_LoadingCount++;
            try
            {
                // 成功卸载会由 YooAsset 释放场景句柄，不再重复 Release。
                await ResourceOperation.WaitAsync(scene.Handle.UnloadSceneAsync(), CancellationToken.None);
                package.Scenes.Remove(sceneAssetName);
            }
            finally
            {
                scene.Unloading = false;
                package.Requests--;
                m_LoadingCount--;
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        private async UniTask LoadSceneCallbackAsync(string sceneAssetName, int priority, LoadSceneCallbacks callbacks, object userData)
        {
            float startTime = Time.realtimeSinceStartup;
            try
            {
                await LoadSceneAsync(sceneAssetName, null, priority,
                    progress => callbacks.LoadSceneUpdateCallback?.Invoke(sceneAssetName, progress, userData), m_ShutdownToken);
            }
            catch (Exception exception)
            {
                if (!m_Shutdown)
                {
                    if (callbacks.LoadSceneFailureCallback == null)
                    {
                        throw;
                    }
                    callbacks.LoadSceneFailureCallback(sceneAssetName, LoadResourceStatus.AssetError, exception.Message, userData);
                }
                return;
            }
            callbacks.LoadSceneSuccessCallback(sceneAssetName, Time.realtimeSinceStartup - startTime, userData);
        }

        private async UniTask UnloadSceneCallbackAsync(string sceneAssetName, UnloadSceneCallbacks callbacks, object userData)
        {
            try
            {
                await UnloadSceneAsync(sceneAssetName, null, m_ShutdownToken);
            }
            catch
            {
                if (!m_Shutdown)
                {
                    if (callbacks.UnloadSceneFailureCallback == null)
                    {
                        throw;
                    }
                    callbacks.UnloadSceneFailureCallback(sceneAssetName, userData);
                }
                return;
            }
            callbacks.UnloadSceneSuccessCallback(sceneAssetName, userData);
        }

        private static AssetInfo GetSceneAssetInfo(ResourcePackage package, string sceneAssetName)
        {
            if (Guid.TryParseExact(sceneAssetName, "N", out _))
            {
                return package.GetAssetInfoByGuid(sceneAssetName);
            }
            return package.GetAssetInfo(sceneAssetName);
        }

        /// <summary>
        /// 场景句柄及其加载和卸载状态。
        /// </summary>
        private sealed class SceneInfo
        {
            internal SceneHandle Handle;
            internal bool Loaded;
            internal bool Unloading;
        }
    }
}
