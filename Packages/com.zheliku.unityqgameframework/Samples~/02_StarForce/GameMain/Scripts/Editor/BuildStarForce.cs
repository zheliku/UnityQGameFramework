using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;

namespace StarForce.Editor
{
    /// <summary>
    /// 构建 StarForce 示例。
    /// </summary>
    public static class BuildStarForce
    {
        /// <summary>
        /// StarForce 启动场景的 GUID。
        /// </summary>
        public const string SceneGuid = "9d2d51bf0fc3dfc42908e3a7a64f4feb";

        /// <summary>
        /// 打开 Player 构建对话框。
        /// </summary>
        [MenuItem("QGF/Examples/02 StarForce/Build Windows Player", false, 420)]
        public static void BuildMenu()
        {
            string output = EditorUtility.SaveFilePanel("Build StarForce", "", "StarForce", "exe");
            if (string.IsNullOrEmpty(output))
            {
                return;
            }
            try
            {
                Debug.Log("StarForce 已构建：" + BuildPlayer(output));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static string BuildResources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before building.");
            }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                throw new InvalidOperationException("Select Windows64 before building StarForce.");
            }
            var settings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(AssetDatabase.GUIDToAssetPath("702e7dd8cbfc56694611d97fd39f3275"));
            ResourceBuildProfile profile = ResourceBuildService.GetProfile(settings, settings.DefaultPackageName);
            // 使用本示例的收集规则，构建过程不切换项目的全局配置。
            string output = ResourceBuildService.GetBundledOutputDirectory(profile);
            ResourceBuildService.Build(profile, false);
            return output;
        }

        /// <summary>
        /// 构建 Windows Player 并部署离线资源包。
        /// </summary>
        /// <param name="output">Player 可执行文件路径。</param>
        /// <returns>Player 可执行文件路径。</returns>
        public static string BuildPlayer(string output)
        {
            if (!string.Equals(Path.GetExtension(output), ".exe", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Choose a Windows .exe output path.", nameof(output));
            }
            output = Path.GetFullPath(output);
            // Unity 首次构建可能补写默认设置，结束时同时恢复内存与磁盘。
            using (var settings = new ProjectSettingsSnapshot())
            {
                return BuildPlayerAndDeploy(output);
            }
        }

        private static string BuildPlayerAndDeploy(string output)
        {
            string resources = BuildResources();
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            // 只把 Launcher 编入 Player；Menu、Main 由资源模块从 Bundle 加载。
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { AssetDatabase.GUIDToAssetPath(SceneGuid) },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = output,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new IOException("StarForce Player build failed.");
            }
            // 仅替换构建产物里的示例目录，不写入项目 Assets/StreamingAssets。
            string data = Path.Combine(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output) + "_Data");
            var resourceSettings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(AssetDatabase.GUIDToAssetPath("702e7dd8cbfc56694611d97fd39f3275"));
            string destination = Path.GetFullPath(Path.Combine(data, "StreamingAssets", "yoo", resourceSettings.DefaultPackageName));
            string expected = Path.GetFullPath(data).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!destination.StartsWith(expected, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Invalid Player resource destination.");
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }
            foreach (string source in Directory.GetFiles(resources, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(destination, Path.GetRelativePath(resources, source));
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(source, target);
            }
            return output;
        }

        private sealed class ProjectSettingsSnapshot : IDisposable
        {
            private readonly (UnityEngine.Object Asset, string Path, string Json, byte[] Bytes)[] entries;

            public ProjectSettingsSnapshot()
            {
                entries = new[] { "PlayerSettings", "GraphicsSettings", "QualitySettings" }.Select(name =>
                {
                    var asset = Unsupported.GetSerializedAssetInterfaceSingleton(name);
                    if (asset == null)
                    {
                        throw new InvalidOperationException("Cannot capture " + name + ".");
                    }
                    string path = "ProjectSettings/" + (name == "PlayerSettings" ? "ProjectSettings" : name) + ".asset";
                    return (asset, path, EditorJsonUtility.ToJson(asset), File.ReadAllBytes(path));
                }).ToArray();
            }

            public void Dispose()
            {
                foreach (var entry in entries)
                {
                    if (EditorJsonUtility.ToJson(entry.Asset) != entry.Json)
                    {
                        EditorJsonUtility.FromJsonOverwrite(entry.Json, entry.Asset);
                        EditorUtility.ClearDirty(entry.Asset);
                    }
                    if (!File.ReadAllBytes(entry.Path).SequenceEqual(entry.Bytes))
                    {
                        File.WriteAllBytes(entry.Path, entry.Bytes);
                    }
                }
            }
        }
    }
}
