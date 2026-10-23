//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System.IO;
using UnityEngine;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Editor.ResourceTools;

namespace StarForce.Editor
{
    public static class FrameworkConfigs
    {
        private static string ConfigDirectory => Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", AssetUtility.GameMainPath, "Configs"));
        [BuildSettingsConfigPath]
        public static string BuildSettingsConfig = Utility.Path.GetRegularPath(Path.Combine(ConfigDirectory, "BuildSettings.toml"));

        [ResourceCollectionConfigPath]
        public static string ResourceCollectionConfig = Utility.Path.GetRegularPath(
            Path.Combine(ConfigDirectory, "ResourceCollection.toml"));

        [ResourceEditorConfigPath]
        public static string ResourceEditorConfig = Utility.Path.GetRegularPath(Path.Combine(ConfigDirectory, "ResourceEditor.toml"));

        [ResourceBuilderConfigPath]
        public static string ResourceBuilderConfig = Utility.Path.GetRegularPath(Path.Combine(ConfigDirectory, "ResourceBuilder.toml"));
    }
}
