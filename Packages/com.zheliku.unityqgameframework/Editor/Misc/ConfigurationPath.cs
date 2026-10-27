using System;
using System.IO;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 取得框架编辑器配置文件的保存位置。
    /// </summary>
    internal static class ConfigurationPath
    {
        // Assets/... remains project-relative; other paths are relative to the configuration file.
        /// <summary>
        /// 将配置中的相对路径解析为项目资产路径。
        /// </summary>
        /// <param name="configuration">配置文件路径。</param>
        /// <param name="path">配置中的相对路径。</param>
        /// <returns>解析后的资产路径。</returns>
        internal static string ResolveAssetPath(string configuration, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("配置中的资源路径不能为空。");
            }

            path = path.Replace('\\', '/');
            string full = Path.GetFullPath(path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal) ? path : Path.Combine(Path.GetDirectoryName(Path.GetFullPath(configuration)), path));
            string assets = Path.GetFullPath(Application.dataPath);
            if (full.Equals(assets, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets";
            }

            if (!full.StartsWith(assets + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("配置中的资源路径必须位于 Assets 内：" + path);
            }

            return "Assets/" + full.Substring(assets.Length + 1).Replace('\\', '/');
        }

        /// <summary>
        /// 将资产路径转换为相对配置文件的路径。
        /// </summary>
        /// <param name="configuration">配置文件路径。</param>
        /// <param name="assetPath">资产路径。</param>
        /// <returns>相对配置路径。</returns>
        internal static string RelativeToConfiguration(string configuration, string assetPath)
        {
            return Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(configuration)), Path.GetFullPath(assetPath)).Replace('\\', '/');
        }
    }
}
