using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 资源构建菜单。
    /// </summary>
    internal static class ResourceMenu
    {
        [MenuItem("QGF/Resource/Build Selected Profile")]
        private static void BuildSelectedProfile()
        {
            ResourceBuildProfile profile = Selection.activeObject as ResourceBuildProfile;
            if (profile == null)
            {
                throw new System.InvalidOperationException("Select a QGF ResourceBuildProfile first.");
            }
            ResourceBuildService.Build(profile, Application.isEditor && profile.Settings.UseEditorSimulation);
        }

        [MenuItem("QGF/Resource/Build Editor Simulation")]
        private static void BuildEditorSimulation()
        {
            ResourceBuildProfile profile = Selection.activeObject as ResourceBuildProfile;
            if (profile == null)
            {
                throw new System.InvalidOperationException("Select a QGF ResourceBuildProfile first.");
            }
            ResourceBuildService.Build(profile, true);
        }

        [MenuItem("QGF/Resource/Build Player Package")]
        private static void BuildPlayerPackage()
        {
            ResourceBuildProfile profile = Selection.activeObject as ResourceBuildProfile;
            if (profile == null)
            {
                throw new System.InvalidOperationException("Select a QGF ResourceBuildProfile first.");
            }
            ResourceBuildService.Build(profile, false);
        }
    }
}
