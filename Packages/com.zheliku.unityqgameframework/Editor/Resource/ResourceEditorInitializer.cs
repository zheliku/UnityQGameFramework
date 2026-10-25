using UnityEditor;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 注册编辑器模拟包的构建入口。
    /// </summary>
    [InitializeOnLoad]
    internal static class ResourceEditorInitializer
    {
        static ResourceEditorInitializer()
        {
            ResourceComponent.EditorSimulationBuilder = BuildSimulation;
        }

        private static string BuildSimulation(ResourceSettings settings, string packageName)
        {
            return ResourceBuildService.Build(ResourceBuildService.GetProfile(settings, packageName), true);
        }
    }
}
