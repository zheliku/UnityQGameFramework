using System;
using System.IO;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    internal static class ConfigurationPath
    {
        // Assets/... remains project-relative; other paths are relative to the configuration file.
        internal static string ResolveAssetPath(string configuration, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("配置中的资源路径不能为空。");
            path = path.Replace('\\', '/');
            string full = Path.GetFullPath(path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal)
                ? path : Path.Combine(Path.GetDirectoryName(Path.GetFullPath(configuration)), path));
            string assets = Path.GetFullPath(Application.dataPath);
            if (full.Equals(assets, StringComparison.OrdinalIgnoreCase)) return "Assets";
            if (!full.StartsWith(assets + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("配置中的资源路径必须位于 Assets 内：" + path);
            return "Assets/" + full.Substring(assets.Length + 1).Replace('\\', '/');
        }

        internal static string RelativeToConfiguration(string configuration, string assetPath) =>
            Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(configuration)), Path.GetFullPath(assetPath)).Replace('\\', '/');
    }
}
