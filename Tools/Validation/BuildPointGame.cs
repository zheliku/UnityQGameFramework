using System;
using System.IO;
using YooAsset.Editor;
using UnityEditor;
using UnityQGameFramework.Editor;

namespace UnityQGameFramework.Examples.PointPointPoint.Editor
{
    /// <summary>
    /// 提供点点点示例的 Player 构建实现。
    /// </summary>
    public static class BuildPointGame
    {
        /// <summary>
        /// 获取点点点示例的启动场景路径。
        /// </summary>
        /// <returns>启动场景路径。</returns>
        public static string GetScenePath()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == "PointPointPoint")
                {
                    return path;
                }
            }
            throw new InvalidOperationException("PointPointPoint scene is missing.");
        }

        /// <summary>
        /// 构建点点点示例 Player 和离线资源包。
        /// </summary>
        /// <param name="output">Player 输出文件路径。</param>
        /// <returns>Player 输出文件路径。</returns>
        public static string BuildPlayer(string output)
        {
            string scene = GetScenePath();
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(scene))).Replace('\\', '/');
            var collector = AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>(root + "/UnityQGameFramework/Resource/YooAssetCollector.asset");
            return ResourceBuildService.BuildPlayer(collector, "QgfExamples.PointPointPoint", scene, output);
        }

    }
}
