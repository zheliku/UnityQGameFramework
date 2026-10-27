using System;
using System.IO;
using YooAsset.Editor;
using UnityEditor;
using UnityQGameFramework.Editor;

namespace StarForce.Editor
{
    /// <summary>
    /// 提供 StarForce 示例的 Player 构建实现。
    /// </summary>
    public static class BuildStarForce
    {
        /// <summary>
        /// 获取 StarForce 示例的启动场景路径。
        /// </summary>
        /// <returns>启动场景路径。</returns>
        public static string GetScenePath()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == "StarForce Launcher")
                {
                    return path;
                }
            }
            throw new InvalidOperationException("StarForce Launcher scene is missing.");
        }

        /// <summary>
        /// 构建 StarForce 示例 Player 和离线资源包。
        /// </summary>
        /// <param name="output">Player 输出文件路径。</param>
        /// <returns>Player 输出文件路径。</returns>
        public static string BuildPlayer(string output)
        {
            string scene = GetScenePath();
            string root = Path.GetDirectoryName(scene).Replace('\\', '/');
            var collector = AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>(root + "/UnityQGameFramework/Resource/YooAssetCollector.asset");
            return ResourceBuildService.BuildPlayer(collector, "QgfExamples.StarForce", scene, output);
        }

    }
}
