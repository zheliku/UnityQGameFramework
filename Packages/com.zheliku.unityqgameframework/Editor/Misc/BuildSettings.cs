//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    internal static class BuildSettings
    {
        [MenuItem("Unity Q Game Framework/Scenes in Build Settings/Default Scenes", false, 20)]
        public static void DefaultScenes() => Apply(false);

        [MenuItem("Unity Q Game Framework/Scenes in Build Settings/All Scenes", false, 21)]
        public static void AllScenes() => Apply(true);

        private static void Apply(bool includeSearchPaths)
        {
            try
            {
                string[] paths = ReadScenePaths(includeSearchPaths);
                // Validate the complete list before replacing the host project's scenes.
                EditorBuildSettings.scenes = Array.ConvertAll(paths, path => new EditorBuildSettingsScene(path, true));
                Debug.Log("QGF 已更新构建场景：" + paths.Length);
            }
            catch (Exception exception) { Debug.LogWarning("QGF 未修改构建场景：" + exception.Message); }
        }

        internal static string[] ReadScenePaths(bool includeSearchPaths)
        {
            string configuration = Type.GetConfigurationPath<BuildSettingsConfigPathAttribute>()
                ?? Path.Combine(Application.dataPath, "UnityQGameFramework/Configs/BuildSettings.xml");
            if (!File.Exists(configuration)) throw new FileNotFoundException("未找到构建场景配置。", configuration);
            var document = new XmlDocument();
            document.Load(configuration);
            XmlNode root = document.SelectSingleNode("/UnityQGameFramework/BuildSettings")
                ?? throw new InvalidDataException("BuildSettings.xml 缺少 BuildSettings 节点。");
            var scenes = new List<string>();
            foreach (XmlNode node in root.SelectNodes("DefaultScenes/DefaultScene"))
            {
                string path = ConfigurationPath.ResolveAssetPath(configuration, node.Attributes?["Name"]?.Value);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) throw new FileNotFoundException("场景不存在。", path);
                if (!scenes.Contains(path)) scenes.Add(path);
            }
            if (includeSearchPaths)
            {
                foreach (XmlNode node in root.SelectNodes("SearchScenePaths/SearchScenePath"))
                {
                    string folder = ConfigurationPath.ResolveAssetPath(configuration, node.Attributes?["Path"]?.Value);
                    if (!AssetDatabase.IsValidFolder(folder)) throw new DirectoryNotFoundException(folder);
                    foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal))
                        if (!scenes.Contains(path)) scenes.Add(path);
                }
            }
            if (scenes.Count == 0) throw new InvalidDataException("配置未指定任何有效场景，不会清空当前场景列表。");
            return scenes.ToArray();
        }
    }
}
