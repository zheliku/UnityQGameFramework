using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityQGameFramework.Core;
using YooAsset;
using SceneHandle = YooAsset.SceneHandle;

namespace UnityQGameFramework.Runtime
{
    internal sealed partial class YooAssetResourceBackend
    {
        /// <summary>
        /// 获取指定资源包中已加载场景的实际名称。
        /// </summary>
        /// <param name="sceneAssetName">场景资产定位地址。</param>
        /// <param name="packageName">资源包名称；为空时使用默认资源包。</param>
        /// <returns>实际场景名称；未加载时返回空。</returns>
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
                throw new GameFrameworkException("Load scene callbacks is invalid.");
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
                throw new GameFrameworkException("Unload scene callbacks is invalid.");
            }
            UnloadSceneCallbackAsync(sceneAssetName, callbacks, userData).Forget();
        }

        /// <summary>
        /// 加载并登记场景，失败或取消时回收本次场景句柄。
        /// </summary>
        /// <param name="sceneAssetName">场景资产定位地址。</param>
        /// <param name="packageName">资源包名称；为空时使用默认资源包。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="progress">加载进度回调。</param>
        /// <param name="cancellationToken">取消异步操作的令牌。</param>
        /// <returns>等待场景加载完成的异步任务。</returns>
        internal async UniTask LoadSceneAsync(string sceneAssetName, string packageName, int priority, Action<float> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }
            PackageInfo package = CreatePackage(packageName);
            if (package.Scenes.ContainsKey(sceneAssetName))
            {
                throw new GameFrameworkException("Scene is already loaded or loading: " + sceneAssetName);
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
                        throw new GameFrameworkException(scene.Handle.IsValid ? scene.Handle.Error : "The scene handle was released during loading.");
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

        /// <summary>
        /// 卸载已登记的场景并移除场景借用。
        /// </summary>
        /// <param name="sceneAssetName">场景资产定位地址。</param>
        /// <param name="packageName">资源包名称；为空时使用默认资源包。</param>
        /// <param name="cancellationToken">取消异步操作的令牌。</param>
        /// <returns>等待场景卸载完成的异步任务。</returns>
        internal async UniTask UnloadSceneAsync(string sceneAssetName, string packageName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            if (!m_Packages.TryGetValue(GetPackageName(packageName), out PackageInfo package)
                || !package.Scenes.TryGetValue(sceneAssetName, out SceneInfo scene) || !scene.Loaded || scene.Unloading)
            {
                throw new GameFrameworkException("Scene is not loaded or is already unloading: " + sceneAssetName);
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

        /// <summary>
        /// 将场景加载结果转发给回调接口。
        /// </summary>
        /// <param name="sceneAssetName">场景资产定位地址。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="callbacks">场景操作回调。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>完成场景加载通知的异步任务。</returns>
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

        /// <summary>
        /// 将场景卸载结果转发给回调接口。
        /// </summary>
        /// <param name="sceneAssetName">场景资产定位地址。</param>
        /// <param name="callbacks">场景操作回调。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>完成场景卸载通知的异步任务。</returns>
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

        /// <summary>
        /// 查询场景资产并检查其是否有效。
        /// </summary>
        /// <param name="package">要查询的资源包。</param>
        /// <param name="sceneAssetName">场景资产定位地址。</param>
        /// <returns>有效的场景资产信息；无效时抛出异常。</returns>
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
            /// <summary>
            /// 本次加载持有的场景句柄。
            /// </summary>
            internal SceneHandle Handle;

            /// <summary>
            /// 场景是否已完成加载。
            /// </summary>
            internal bool Loaded;

            /// <summary>
            /// 场景是否正在卸载。
            /// </summary>
            internal bool Unloading;
        }
    }
}
