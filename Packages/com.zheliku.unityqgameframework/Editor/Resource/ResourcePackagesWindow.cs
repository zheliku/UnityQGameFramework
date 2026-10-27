using UnityEditor;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 打开 YooAsset 原生资源窗口。
    /// </summary>
    internal static class ResourcePackagesWindow
    {
        /// <summary>
        /// 打开 YooAsset 原生资源收集窗口。
        /// </summary>
        [MenuItem("QGF/Resource/Packages", false, 100)]
        internal static void Open()
        {
            ResourceCollectorIntegration.OpenCollector(null);
        }

    }
}
