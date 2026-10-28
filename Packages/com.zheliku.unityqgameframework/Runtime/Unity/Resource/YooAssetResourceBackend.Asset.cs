using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityQGameFramework.Core;
using YooAsset;

namespace UnityQGameFramework.Runtime
{
    internal sealed partial class YooAssetResourceBackend
    {
        /// <summary>
        /// 检查默认资源包是否包含指定资产。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <returns>资产存在状态。</returns>
        public HasAssetResult HasAsset(string assetName)
        {
            ThrowIfShutdown();
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }
            if (!m_Packages.TryGetValue(m_Settings.DefaultPackageName, out PackageInfo package) || !package.Ready || package.Destroying)
            {
                // 查询本身不能异步等待；实际加载会初始化默认包并返回准确的失败信息。
                return HasAssetResult.AssetOnDisk;
            }
            if (!GetAssetInfo(package.Package, assetName, null).IsValid)
            {
                return HasAssetResult.NotExist;
            }
            return IsRawPackage(package.Package) ? HasAssetResult.BinaryOnDisk : HasAssetResult.AssetOnDisk;
        }

        /// <summary>
        /// 异步加载默认资源包中的资产。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="assetType">资产类型。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="callbacks">加载回调。</param>
        /// <param name="userData">用户数据。</param>
        public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks callbacks, object userData)
        {
            if (callbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }
            LoadAssetCallbackAsync(assetName, assetType, priority, callbacks, userData).Forget();
        }

        /// <summary>
        /// 归还默认资源包中借用的资产。
        /// </summary>
        /// <param name="asset">要归还的资产。</param>
        public void UnloadAsset(object asset)
        {
            UnloadAsset(asset, null);
        }

        /// <summary>
        /// 归还指定资源包中的一次隐式资产借用。
        /// </summary>
        /// <param name="asset">要归还的资产。</param>
        /// <param name="packageName">资源包名称；为空时使用默认资源包。</param>
        internal void UnloadAsset(object asset, string packageName)
        {
            ThrowIfShutdown();
            if (asset == null)
            {
                throw new GameFrameworkException("Asset is invalid.");
            }
            if (m_Packages.TryGetValue(GetPackageName(packageName), out PackageInfo package))
            {
                for (int i = package.Assets.Count - 1; i >= 0; i--)
                {
                    AssetBorrow borrow = package.Assets[i];
                    if (borrow.Implicit && borrow.Handle.IsValid && ReferenceEquals(borrow.Handle.AssetObject, asset))
                    {
                        borrow.Handle.Release();
                        package.Assets.RemoveAt(i);
                        return;
                    }
                }
            }
            throw new GameFrameworkException("The asset has no outstanding borrow in this resource package.");
        }

        /// <summary>
        /// 初始化资源包并加载资产，失败或取消时释放本次句柄。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="assetType">要加载的 Unity 资产类型。</param>
        /// <param name="packageName">资源包名称；为空时使用默认资源包。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="implicitBorrow">是否登记为可通过资产对象归还的借用。</param>
        /// <param name="cancellationToken">取消异步操作的令牌。</param>
        /// <returns>加载成功的资产句柄；显式借用由调用方释放。</returns>
        internal UniTask<AssetHandle> LoadAssetAsync(string assetName, Type assetType, string packageName, int priority, bool implicitBorrow, CancellationToken cancellationToken)
        {
            return LoadAssetAsync(assetName, assetType, packageName, priority, implicitBorrow, null, cancellationToken);
        }

        /// <summary>
        /// 初始化资源包并加载资产，失败或取消时释放本次句柄。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="assetType">要加载的 Unity 资产类型。</param>
        /// <param name="packageName">资源包名称；为空时使用默认资源包。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="implicitBorrow">是否登记为可通过资产对象归还的借用。</param>
        /// <param name="progress">加载进度回调。</param>
        /// <param name="cancellationToken">取消异步操作的令牌。</param>
        /// <returns>加载成功的资产句柄；显式借用由调用方释放。</returns>
        private async UniTask<AssetHandle> LoadAssetAsync(string assetName, Type assetType, string packageName, int priority, bool implicitBorrow, Action<float> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfShutdown();
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }
            if (assetType != null && !typeof(UnityEngine.Object).IsAssignableFrom(assetType))
            {
                throw new GameFrameworkException("Asset type must derive from UnityEngine.Object.");
            }

            PackageInfo package = CreatePackage(packageName);
            package.Requests++;
            m_LoadingCount++;
            AssetHandle handle = null;
            bool succeeded = false;
            try
            {
                using (var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, m_ShutdownToken))
                {
                    await InitializePackageAsync(package.Package.PackageName, cancellation.Token);
                    cancellation.Token.ThrowIfCancellationRequested();
                    handle = package.Package.LoadAssetAsync(GetAssetInfo(package.Package, assetName, assetType), (uint)Math.Max(priority, 0));
                    while (handle.IsValid && !handle.IsDone)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        progress?.Invoke(handle.Progress);
                        await UniTask.NextFrame(cancellation.Token);
                    }
                    cancellation.Token.ThrowIfCancellationRequested();
                    if (!handle.IsValid || handle.Status != EOperationStatus.Succeeded || handle.AssetObject == null)
                    {
                        throw new GameFrameworkException(handle.IsValid ? handle.Error : "The resource handle was released during loading.");
                    }
                    progress?.Invoke(1f);
                    cancellation.Token.ThrowIfCancellationRequested();
                    // 每次借用保存自己的句柄；同一 Target 的其他借用不受本次归还影响。
                    package.Assets.Add(new AssetBorrow(handle, implicitBorrow));
                    succeeded = true;
                    return handle;
                }
            }
            finally
            {
                if (!succeeded && handle != null && handle.IsValid)
                {
                    handle.Release();
                }
                package.Requests--;
                m_LoadingCount--;
            }
        }

        /// <summary>
        /// 将资产异步加载结果转发给回调接口。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="assetType">要加载的 Unity 资产类型。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="callbacks">加载结果及进度回调。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>完成回调通知的异步任务。</returns>
        private async UniTask LoadAssetCallbackAsync(string assetName, Type assetType, int priority, LoadAssetCallbacks callbacks, object userData)
        {
            float startTime = Time.realtimeSinceStartup;
            AssetHandle handle;
            try
            {
                handle = await LoadAssetAsync(assetName, assetType, null, priority, true,
                    progress => callbacks.LoadAssetUpdateCallback?.Invoke(assetName, progress, userData), m_ShutdownToken);
            }
            catch (Exception exception)
            {
                if (!m_Shutdown)
                {
                    if (callbacks.LoadAssetFailureCallback == null)
                    {
                        throw;
                    }
                    callbacks.LoadAssetFailureCallback(assetName, LoadResourceStatus.AssetError, exception.Message, userData);
                }
                return;
            }
            // 业务回调的异常直接上报，不能再触发一次加载失败回调。
            callbacks.LoadAssetSuccessCallback(assetName, handle.AssetObject, Time.realtimeSinceStartup - startTime, userData);
        }

        /// <summary>
        /// 异步读取默认资源包中的二进制内容。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="callbacks">加载回调。</param>
        /// <param name="userData">用户数据。</param>
        public void LoadBinary(string assetName, LoadBinaryCallbacks callbacks, object userData)
        {
            if (callbacks == null)
            {
                throw new GameFrameworkException("Load binary callbacks is invalid.");
            }
            LoadBinaryCallbackAsync(assetName, callbacks, userData).Forget();
        }

        /// <summary>
        /// 读取二进制内容并在完成后释放资产句柄。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="packageName">资源包名称；为空时使用默认资源包。</param>
        /// <param name="cancellationToken">取消异步操作的令牌。</param>
        /// <returns>拥有独立生命周期的二进制数据。</returns>
        internal async UniTask<byte[]> LoadBinaryAsync(string assetName, string packageName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ResourcePackage package = await InitializePackageAsync(packageName, cancellationToken);
            Type type = IsRawPackage(package) ? typeof(RawFileObject) : typeof(TextAsset);
            AssetHandle handle = await LoadAssetAsync(assetName, type, packageName, 0, false, cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return GetBytes(handle.AssetObject);
            }
            finally
            {
                if (handle.IsValid)
                {
                    handle.Release();
                }
            }
        }

        /// <summary>
        /// 同步读取已初始化资源包中的二进制内容。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <returns>拥有独立生命周期的文件内容。</returns>
        public byte[] LoadBinary(string assetName)
        {
            ThrowIfShutdown();
            if (!m_Packages.TryGetValue(m_Settings.DefaultPackageName, out PackageInfo package) || !package.Ready || package.Destroying)
            {
                throw new GameFrameworkException("Initialize the resource package before synchronous loading.");
            }
            Type type = IsRawPackage(package.Package) ? typeof(RawFileObject) : typeof(TextAsset);
            AssetHandle handle = package.Package.LoadAssetSync(GetAssetInfo(package.Package, assetName, type));
            try
            {
                if (handle.Status != EOperationStatus.Succeeded)
                {
                    throw new GameFrameworkException(handle.Error);
                }
                return GetBytes(handle.AssetObject);
            }
            finally
            {
                if (handle.IsValid)
                {
                    handle.Release();
                }
            }
        }

        /// <summary>
        /// 获取二进制内容长度。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <returns>内容长度。</returns>
        public int GetBinaryLength(string assetName)
        {
            return LoadBinary(assetName).Length;
        }

        /// <summary>
        /// 同步读取二进制内容片段。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="offset">起始偏移。</param>
        /// <param name="buffer">目标缓冲区。</param>
        /// <param name="startIndex">缓冲区起始索引。</param>
        /// <param name="length">要读取的字节数。</param>
        /// <returns>实际读取的字节数。</returns>
        public int LoadBinarySegment(string assetName, int offset, byte[] buffer, int startIndex, int length)
        {
            if (buffer == null || offset < 0 || startIndex < 0 || length < 0 || startIndex > buffer.Length - length)
            {
                throw new GameFrameworkException("Binary segment arguments are invalid.");
            }
            byte[] bytes = LoadBinary(assetName);
            int count = Math.Min(length, Math.Max(bytes.Length - offset, 0));
            Buffer.BlockCopy(bytes, Math.Min(offset, bytes.Length), buffer, startIndex, count);
            return count;
        }

        /// <summary>
        /// 将二进制读取结果转发给回调接口。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="callbacks">加载结果及进度回调。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>完成回调通知的异步任务。</returns>
        private async UniTask LoadBinaryCallbackAsync(string assetName, LoadBinaryCallbacks callbacks, object userData)
        {
            float startTime = Time.realtimeSinceStartup;
            byte[] bytes;
            try
            {
                bytes = await LoadBinaryAsync(assetName, null, m_ShutdownToken);
            }
            catch (Exception exception)
            {
                if (!m_Shutdown)
                {
                    if (callbacks.LoadBinaryFailureCallback == null)
                    {
                        throw;
                    }
                    callbacks.LoadBinaryFailureCallback(assetName, LoadResourceStatus.AssetError, exception.Message, userData);
                }
                return;
            }
            callbacks.LoadBinarySuccessCallback(assetName, bytes, Time.realtimeSinceStartup - startTime, userData);
        }

        /// <summary>
        /// 从文本资产或原始文件资产读取字节。
        /// </summary>
        /// <param name="asset">TextAsset 或 RawFileObject 资产。</param>
        /// <returns>读取的字节数组。</returns>
        private static byte[] GetBytes(UnityEngine.Object asset)
        {
            if (asset is RawFileObject rawFile)
            {
                return rawFile.GetBytes();
            }
            if (asset is TextAsset textAsset)
            {
                return textAsset.bytes;
            }
            throw new GameFrameworkException("Binary assets must be TextAsset or RawFileObject.");
        }

        /// <summary>
        /// 判断资源包是否使用原始文件构建。
        /// </summary>
        /// <param name="package">要查询的资源包。</param>
        /// <returns>资源包是否使用原始文件或其模拟构建类型。</returns>
        private static bool IsRawPackage(ResourcePackage package)
        {
            int type = package.GetPackageDetails().BuildBundleType;
            return type == (int)EBundleType.RawBundle || type == (int)EBundleType.VirtualRawBundle;
        }

        /// <summary>
        /// 按定位地址或 GUID 查询资产信息。
        /// </summary>
        /// <param name="package">要查询的资源包。</param>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="assetType">要加载的 Unity 资产类型。</param>
        /// <returns>YooAsset 资产信息。</returns>
        private static YooAsset.AssetInfo GetAssetInfo(ResourcePackage package, string assetName, Type assetType)
        {
            if (Guid.TryParseExact(assetName, "N", out _))
            {
                return package.GetAssetInfoByGuid(assetName, assetType);
            }
            return package.GetAssetInfo(assetName, assetType);
        }

        /// <summary>
        /// 一次资产借用。
        /// </summary>
        private sealed class AssetBorrow
        {
            /// <summary>
            /// 本次借用持有的资产句柄。
            /// </summary>
            internal readonly AssetHandle Handle;

            /// <summary>
            /// 是否允许按资产对象归还本次借用。
            /// </summary>
            internal readonly bool Implicit;

            /// <summary>
            /// 登记一次资产借用及其归还方式。
            /// </summary>
            /// <param name="handle">本次加载持有的资产句柄。</param>
            /// <param name="implicitBorrow">是否登记为可通过资产对象归还的借用。</param>
            internal AssetBorrow(AssetHandle handle, bool implicitBorrow)
            {
                Handle = handle;
                Implicit = implicitBorrow;
            }
        }
    }
}
