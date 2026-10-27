using UnityEditor;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 注册编辑器资源包的构建入口。
    /// </summary>
    [InitializeOnLoad]
    internal static class ResourceEditorInitializer
    {
        /// <summary>
        /// 注册 QGF 资源构建编辑器回调。
        /// </summary>
        static ResourceEditorInitializer()
        {
            ResourceComponent.EditorPackageBuilder = BuildPackage;
        }

        /// <summary>
        /// 使用指定设置构建资源包。
        /// </summary>
        /// <param name="settings">QGF 资源设置。</param>
        /// <param name="packageName">资源包名称。</param>
        /// <param name="simulate">是否构建编辑器模拟包。</param>
        /// <returns>构建输出目录。</returns>
        private static string BuildPackage(ResourceSettings settings, string packageName, bool simulate)
        {
            return ResourceBuildService.Build(ResourceBuildService.GetCollector(settings), packageName, simulate);
        }
    }
}
