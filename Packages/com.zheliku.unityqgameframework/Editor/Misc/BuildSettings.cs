//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tomlyn;
using Tomlyn.Model;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 管理构建场景列表。
    /// </summary>
    internal static class BuildSettings
    {
        /// <summary>
        /// 将默认示例场景写入 Build Settings。
        /// </summary>
        [MenuItem("QGF/Scenes/Default Scenes", false, 20)]
        public static void DefaultScenes()
        {
            Apply(false);
        }

        /// <summary>
        /// 将所有示例场景写入 Build Settings。
        /// </summary>
        [MenuItem("QGF/Scenes/All Scenes", false, 21)]
        public static void AllScenes()
        {
            Apply(true);
        }

        /// <summary>
        /// 按筛选规则应用 Build Settings。
        /// </summary>
        /// <param name="includeSearchPaths">是否包含搜索路径中的场景。</param>
        private static void Apply(bool includeSearchPaths)
        {
            try
            {
                string[] paths = ReadScenePaths(includeSearchPaths);
                // Validate the complete list before replacing the host project's scenes.
                EditorBuildSettings.scenes = Array.ConvertAll(paths, path => new EditorBuildSettingsScene(path, true));
                Debug.Log("QGF build scenes updated: " + paths.Length);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("QGF build scenes were not changed: " + exception.Message);
            }
        }

        /// <summary>
        /// 读取要加入 Build Settings 的场景路径。
        /// </summary>
        /// <param name="includeSearchPaths">是否包含搜索路径中的场景。</param>
        /// <returns>场景路径数组。</returns>
        internal static string[] ReadScenePaths(bool includeSearchPaths)
        {
            string configuration = Type.GetConfigurationPath<BuildSettingsConfigPathAttribute>() ?? Path.Combine(Application.dataPath, "UnityQGameFramework/Configs/BuildSettings.toml");
            if (!File.Exists(configuration))
            {
                throw new FileNotFoundException("Build scene configuration was not found.", configuration);
            }

            var model = Toml.ToModel(File.ReadAllText(configuration));
            if (!model.TryGetValue("BuildSettings", out object value) || !(value is TomlTable settings))
            {
                throw new InvalidDataException("TOML 'BuildSettings' must be a table.");
            }

            var scenes = new List<string>();
            foreach (string scene in ReadPaths(settings, "DefaultScenes"))
            {
                string path = ResolveAssetPath(configuration, scene);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    throw new FileNotFoundException("The scene does not exist.", path);
                }

                if (!scenes.Contains(path))
                {
                    scenes.Add(path);
                }
            }

            if (includeSearchPaths)
            {
                foreach (string directory in ReadPaths(settings, "SearchScenePaths"))
                {
                    string folder = ResolveAssetPath(configuration, directory);
                    if (!AssetDatabase.IsValidFolder(folder))
                    {
                        throw new DirectoryNotFoundException(folder);
                    }

                    foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal))
                    {
                        if (!scenes.Contains(path))
                        {
                            scenes.Add(path);
                        }
                    }
                }
            }

            if (scenes.Count == 0)
            {
                throw new InvalidDataException("The configuration contains no valid scenes. The current scene list was preserved.");
            }

            return scenes.ToArray();
        }

        /// <summary>
        /// 读取场景配置中的路径数组。
        /// </summary>
        /// <param name="settings">构建场景配置。</param>
        /// <param name="key">路径数组的配置键。</param>
        /// <returns>路径数组；配置键缺失时返回空数组。</returns>
        private static string[] ReadPaths(TomlTable settings, string key)
        {
            if (!settings.TryGetValue(key, out object value))
            {
                return Array.Empty<string>();
            }

            if (!(value is TomlArray array) || array.Any(item => !(item is string)))
            {
                throw new InvalidDataException("TOML '" + key + "' must be a string array.");
            }

            return array.Cast<string>().ToArray();
        }

        /// <summary>
        /// 将配置中的相对路径解析为项目资产路径。
        /// </summary>
        /// <param name="configuration">构建配置文件路径。</param>
        /// <param name="path">项目路径或相对于配置文件的路径。</param>
        /// <returns>位于 Assets 内的项目资产路径。</returns>
        private static string ResolveAssetPath(string configuration, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The configured asset path must not be empty.");
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
                throw new ArgumentException("The configured asset path must be inside Assets: " + path);
            }

            return "Assets/" + full.Substring(assets.Length + 1).Replace('\\', '/');
        }
    }
}
