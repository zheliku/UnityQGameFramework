using YooAsset;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 资源包的自定义初始化扩展。
    /// </summary>
    public interface IResourcePackageInitializer
    {
        /// <summary>
        /// 创建 YooAsset 文件系统初始化选项。
        /// </summary>
        /// <param name="settings">资源包配置。</param>
        /// <param name="builtinRoot">当前包的内置资源目录。</param>
        /// <param name="cacheRoot">当前包的下载缓存目录。</param>
        /// <returns>本次初始化使用的官方选项。</returns>
        InitializePackageOptions CreateOptions(ResourcePackageSettings settings, string builtinRoot, string cacheRoot);
    }
}
