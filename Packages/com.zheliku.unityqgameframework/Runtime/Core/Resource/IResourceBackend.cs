using System;

namespace UnityQGameFramework.Core
{
    /// <summary>
    /// 资源后端接口。
    /// </summary>
    public interface IResourceBackend
    {
        /// <summary>
        /// 获取资源只读路径。
        /// </summary>
        string ReadOnlyPath
        {
            get;
        }

        /// <summary>
        /// 获取资源读写路径。
        /// </summary>
        string ReadWritePath
        {
            get;
        }

        /// <summary>
        /// 获取资源资产数量。
        /// </summary>
        int AssetCount
        {
            get;
        }

        /// <summary>
        /// 获取正在执行的资源请求数量。
        /// </summary>
        int LoadingCount
        {
            get;
        }

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
        /// <param name="assetType">资产类型；为空时由资源后端推断。</param>
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
