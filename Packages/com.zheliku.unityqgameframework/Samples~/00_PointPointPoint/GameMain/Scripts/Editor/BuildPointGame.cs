using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.PointPointPoint.Editor
{
    /// <summary>
    /// 构建点点点示例。
    /// </summary>
    public static class BuildPointGame
    {
        /// <summary>
        /// 点点点启动场景的 GUID。
        /// </summary>
        public const string SceneGuid = "f99fe5fffa1724e4ab21a8936d44ab7e";

        /// <summary>
        /// 打开 Player 构建对话框。
        /// </summary>
        [MenuItem("QGF/Examples/PointPointPoint/Build Windows Player", false, 410)]
        public static void BuildMenu()
        {
            string output = EditorUtility.SaveFilePanel("Build PointPointPoint", "", "PointPointPoint", "exe");
            if (string.IsNullOrEmpty(output))
            {
                return;
            }
            try
            {
                Debug.Log("PointPointPoint 已构建：" + BuildPlayer(output));
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
                throw new InvalidOperationException("Select Windows64 before building PointPointPoint.");
            }
            var settings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(AssetDatabase.GUIDToAssetPath("b8df0efa960c5fde5d4475dcf00a8c2c"));
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
            // Unity 首次构建会补写默认项，结束时恢复宿主原来的设置。
            using (var settings = new ProjectSettingsSnapshot())
            {
                return BuildPlayerAndDeploy(output);
            }
        }

        private static string BuildPlayerAndDeploy(string output)
        {
            string resources = BuildResources();
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            // 直接传场景列表，不改 Build Settings、图形管线或 Player Settings。
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { AssetDatabase.GUIDToAssetPath(SceneGuid) },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = output,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new IOException("PointPointPoint Player build failed.");
            }
            // 构建完成后部署到 Player 输出，项目的 Assets/StreamingAssets 不参与。
            string data = Path.Combine(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output) + "_Data");
            var resourceSettings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(AssetDatabase.GUIDToAssetPath("b8df0efa960c5fde5d4475dcf00a8c2c"));
            string destination = Path.GetFullPath(Path.Combine(data, "StreamingAssets", "yoo", resourceSettings.DefaultPackageName));
            string expected = Path.GetFullPath(data).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!destination.StartsWith(expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Invalid Player resource destination.");
            }
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
                    // 两处都恢复，避免退出编辑器时又把构建后的内存状态写回项目。
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
