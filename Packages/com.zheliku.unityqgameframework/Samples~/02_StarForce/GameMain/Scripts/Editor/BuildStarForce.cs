using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Editor.ResourceTools;

namespace StarForce.Editor
{
    public static class BuildStarForce
    {
        public const string SceneGuid = "9d2d51bf0fc3dfc42908e3a7a64f4feb";

        [MenuItem("QGF/Examples/02 StarForce/Build Windows Player", false, 420)]
        public static void BuildMenu()
        {
            string output = EditorUtility.SaveFilePanel("Build StarForce", "", "StarForce", "exe");
            if (string.IsNullOrEmpty(output)) return;
            try { Debug.Log("StarForce 已构建：" + BuildPlayer(output)); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private static string BuildResources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before building.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Select Windows64 before building StarForce.");
            string scene = AssetDatabase.GUIDToAssetPath(SceneGuid);
            if (string.IsNullOrEmpty(scene)) throw new FileNotFoundException("Import the complete StarForce sample.");
            string gameMain = Path.GetDirectoryName(scene).Replace('\\', '/') + "/GameMain";
            // Unity 的场景 Bundle 不能输出到 Library，使用编辑器忽略的独立构建目录。
            string root = Path.GetFullPath("QgfBuild~/StarForce/" + Application.unityVersion);
            Directory.CreateDirectory(root);
            // 集合记录资产 GUID，移动整个 Sample 后仍指向同一批资产。
            string collection = Path.Combine(root, "ResourceCollection.toml");
            File.Copy(gameMain + "/Configs/ResourceCollection.toml", collection, true);
            // 使用独立配置，不读取或保存宿主资源工具的全局构建选项。
            var builder = new ResourceBuilderController(Path.Combine(root, "ResourceBuilder.toml"), collection)
            {
                InternalResourceVersion = 1,
                Platforms = Platform.Windows64,
                AssetBundleCompression = AssetBundleCompressionType.LZ4,
                CompressionHelperTypeName = "UnityQGameFramework.Runtime.DefaultCompressionHelper",
                OutputDirectory = Path.Combine(root, "Output"),
                OutputPackageSelected = true,
                OutputFullSelected = false,
                OutputPackedSelected = false
            };
            Directory.CreateDirectory(builder.OutputDirectory);
            // 构建器会清理自己的输出，先确认目标都在本例的构建目录内。
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (string path in new[] { builder.WorkingPath, builder.OutputPackagePath, builder.BuildReportPath })
                if (!Path.GetFullPath(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("StarForce build output escaped its root.");
            if (!builder.RefreshCompressionHelper() || !builder.BuildResources())
                throw new IOException("StarForce resource build failed. See " + builder.BuildReportPath);
            return Path.GetFullPath(builder.OutputPackagePath + "Windows64");
        }

        public static string BuildPlayer(string output)
        {
            if (!string.Equals(Path.GetExtension(output), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose a Windows .exe output path.", nameof(output));
            output = Path.GetFullPath(output);
            // Unity 首次构建可能补写默认设置，结束时同时恢复内存与磁盘。
            using (var settings = new ProjectSettingsSnapshot()) return BuildPlayerAndDeploy(output);
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
            if (report.summary.result != BuildResult.Succeeded) throw new IOException("StarForce Player build failed.");
            // 仅替换构建产物里的示例目录，不写入项目 Assets/StreamingAssets。
            string data = Path.Combine(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output) + "_Data");
            string destination = Path.GetFullPath(Path.Combine(data, "StreamingAssets", GameEntry.ResourceFolder));
            string expected = Path.GetFullPath(data).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!destination.StartsWith(expected, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Invalid Player resource destination.");
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
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
                    if (asset == null) throw new InvalidOperationException("Cannot capture " + name + ".");
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
                    if (!File.ReadAllBytes(entry.Path).SequenceEqual(entry.Bytes)) File.WriteAllBytes(entry.Path, entry.Bytes);
                }
            }
        }
    }
}
