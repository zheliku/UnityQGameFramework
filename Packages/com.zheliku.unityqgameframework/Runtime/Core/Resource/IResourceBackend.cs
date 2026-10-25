using System;

namespace UnityQGameFramework.Core.Resource
{
    /// <summary>
    /// 资源后端接口。
    /// </summary>
    public interface IResourceBackend
    {
        /// <summary>
        /// 获取资源只读路径。
        /// </summary>
        string ReadOnlyPath { get; }

        /// <summary>
        /// 获取资源读写路径。
        /// </summary>
        string ReadWritePath { get; }

        /// <summary>
        /// 获取资源运行模式。
        /// </summary>
        ResourceMode ResourceMode { get; }

        /// <summary>
        /// 获取资源资产数量。
        /// </summary>
        int AssetCount { get; }

        /// <summary>
        /// 获取正在加载的资产数量。
        /// </summary>
        int LoadWorkingAgentCount { get; }

        /// <summary>
        /// 获取原生文件长度。
        /// </summary>
        /// <param name="assetName">原生文件定位地址。</param>
        /// <returns>文件长度。</returns>
        int GetBinaryLength(string assetName);

        /// <summary>
        /// 初始化默认资源包。
        /// </summary>
        /// <param name="callback">初始化完成回调。</param>
        void Initialize(InitResourcesCompleteCallback callback);

        /// <summary>
        /// 设置资源目录。
        /// </summary>
        /// <param name="readOnlyPath">资源只读目录。</param>
        /// <param name="readWritePath">资源读写目录。</param>
        void SetPaths(string readOnlyPath, string readWritePath);

        /// <summary>
        /// 检查资产是否存在于当前资源清单。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <returns>资产存在状态。</returns>
        HasAssetResult HasAsset(string assetName);

        /// <summary>
        /// 异步加载资产。
        /// </summary>
        /// <param name="assetName">资产定位地址。</param>
        /// <param name="assetType">资产类型。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="callbacks">加载回调。</param>
        /// <param name="userData">用户数据。</param>
        void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks callbacks, object userData);

        /// <summary>
        /// 归还已加载资产。
        /// </summary>
        /// <param name="asset">要归还的资产。</param>
        void UnloadAsset(object asset);

        /// <summary>
        /// 异步加载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景定位地址。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="callbacks">加载回调。</param>
        /// <param name="userData">用户数据。</param>
        void LoadScene(string sceneAssetName, int priority, LoadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 异步卸载场景。
        /// </summary>
        /// <param name="sceneAssetName">场景定位地址。</param>
        /// <param name="callbacks">卸载回调。</param>
        /// <param name="userData">用户数据。</param>
        void UnloadScene(string sceneAssetName, UnloadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 异步加载原生文件。
        /// </summary>
        /// <param name="assetName">原生文件定位地址。</param>
        /// <param name="callbacks">加载回调。</param>
        /// <param name="userData">用户数据。</param>
        void LoadBinary(string assetName, LoadBinaryCallbacks callbacks, object userData);

        /// <summary>
        /// 同步读取原生文件。
        /// </summary>
        /// <param name="assetName">原生文件定位地址。</param>
        /// <returns>文件内容。</returns>
        byte[] LoadBinary(string assetName);

        /// <summary>
        /// 同步读取原生文件片段。
        /// </summary>
        /// <param name="assetName">原生文件定位地址。</param>
        /// <param name="offset">起始偏移。</param>
        /// <param name="buffer">目标缓冲区。</param>
        /// <param name="startIndex">缓冲区起始索引。</param>
        /// <param name="length">要读取的字节数。</param>
        /// <returns>实际读取的字节数。</returns>
        int LoadBinarySegment(string assetName, int offset, byte[] buffer, int startIndex, int length);

        /// <summary>
        /// 释放未使用的资产。
        /// </summary>
        void UnloadUnusedAssets();

        /// <summary>
        /// 轮询资源后端。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间。</param>
        /// <param name="realElapseSeconds">真实流逝时间。</param>
        void Update(float elapseSeconds, float realElapseSeconds);

        /// <summary>
        /// 关闭资源后端。
        /// </summary>
        void Shutdown();
    }
}
